using Makaretu.Dns;
using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot
{
    public sealed class MachineFactory : IDisposable
    {
        public const string MdnsServiceName = "_makerbot-jsonrpc._tcp";
        private const int TargetPort = 12307;
        private const int ListenPort = 12308;
        private const int SourcePort = 12309;
        private readonly ILogger<MachineFactory> _logger;
        private bool _disposed;

        public MachineFactory(ILoggerFactory logFactory = null) => _logger = logFactory?.CreateLogger<MachineFactory>();

        public Broadcast[] Discover() => DiscoverAsync().GetAwaiter().GetResult();

        public async Task<Broadcast[]> DiscoverAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var duration = timeout ?? TimeSpan.FromSeconds(4);
            var results = new List<Broadcast>();
            try { results.AddRange(await DiscoverMdnsAsync(duration, cancellationToken).ConfigureAwait(false)); }
            catch (Exception ex) { _logger?.LogWarning(ex, "MakerBot mDNS discovery failed; trying legacy UDP discovery"); }
            try { results.AddRange(await DiscoverUdpAsync(duration, cancellationToken).ConfigureAwait(false)); }
            catch (Exception ex) { _logger?.LogWarning(ex, "MakerBot legacy UDP discovery failed"); }
            return MergeDiscoveries(results);
        }

        internal static Broadcast[] MergeDiscoveries(IEnumerable<Broadcast> results)
        {
            return (results ?? Enumerable.Empty<Broadcast>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.iserial))
                .GroupBy(x => x.iserial, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(AddressPreference).First())
                .ToArray();
        }

        private static int AddressPreference(Broadcast discovery)
        {
            if (!IPAddress.TryParse(discovery.ip, out var address)) return 2;
            return address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1;
        }

        public async Task<Broadcast> DiscoverBySerialAsync(string serialNumber, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("Serial number is required.", nameof(serialNumber));
            return (await DiscoverAsync(timeout, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(x => string.Equals(x.iserial, serialNumber, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Broadcast[]> DiscoverMdnsAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var records = new List<ResourceRecord>();
            var instances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var mdns = new MulticastService())
            using (var discovery = new ServiceDiscovery(mdns))
            {
                discovery.ServiceInstanceDiscovered += (sender, args) =>
                {
                    lock (instances) instances.Add(args.ServiceInstanceName.ToString());
                    mdns.SendQuery(args.ServiceInstanceName, type: DnsType.ANY);
                };
                mdns.AnswerReceived += (sender, args) =>
                {
                    lock (records)
                    {
                        records.AddRange(args.Message.Answers);
                        records.AddRange(args.Message.AdditionalRecords);
                    }
                };
                mdns.Start();
                discovery.QueryServiceInstances(MdnsServiceName);
                await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
                mdns.Stop();
            }

            lock (records)
            {
                var addresses = records.OfType<AddressRecord>().GroupBy(x => x.Name.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.OrderBy(record => record.Address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).First().Address, StringComparer.OrdinalIgnoreCase);
                var text = records.OfType<TXTRecord>().GroupBy(x => x.Name.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => ParseTxt(x.First().Strings), StringComparer.OrdinalIgnoreCase);
                var output = new List<Broadcast>();
                foreach (var service in records.OfType<SRVRecord>())
                {
                    if (!addresses.TryGetValue(service.Target.ToString(), out var address)) continue;
                    text.TryGetValue(service.Name.ToString(), out var properties);
                    properties = properties ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    output.Add(new Broadcast
                    {
                        ip = address.ToString(),
                        port = service.Port.ToString(),
                        machine_name = Get(properties, "machine_name"),
                        machine_type = Get(properties, "machine_type"),
                        iserial = Get(properties, "iserial"),
                        api_version = Get(properties, "api_version"),
                        bot_type = Get(properties, "bot_type"),
                        firmware_version = ParseFirmware(Get(properties, "firmware_version")),
                        motor_driver_version = Get(properties, "motor_driver_version"),
                        ssl_port = Get(properties, "ssl_port") ?? "12309",
                        vid = ParseInt(Get(properties, "vid")),
                        pid = ParseInt(Get(properties, "pid"))
                    });
                }
                return output.ToArray();
            }
        }

        private static Dictionary<string, string> ParseTxt(IEnumerable<string> values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                var separator = value.IndexOf('=');
                if (separator > 0) result[value.Substring(0, separator)] = value.Substring(separator + 1);
            }
            return result;
        }

        private static string Get(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : null;
        private static int ParseInt(string value) => int.TryParse(value, out var parsed) ? parsed : 0;
        private static Firmware_Version ParseFirmware(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var parts = value.Split('.');
            return new Firmware_Version
            {
                major = parts.Length > 0 ? ParseInt(parts[0]) : 0,
                minor = parts.Length > 1 ? ParseInt(parts[1]) : 0,
                bugfix = parts.Length > 2 ? ParseInt(parts[2]) : 0,
                build = parts.Length > 3 ? ParseInt(parts[3]) : 0
            };
        }

        private async Task<Broadcast[]> DiscoverUdpAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var results = new List<Broadcast>();
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                sender.Bind(new IPEndPoint(IPAddress.Any, SourcePort));
                receiver.Bind(new IPEndPoint(IPAddress.Any, ListenPort));
                receiver.Blocking = false;
                var payload = Encoding.UTF8.GetBytes("{\"command\":\"broadcast\"}");
                sender.SendTo(payload, new IPEndPoint(IPAddress.Broadcast, TargetPort));
                var deadline = DateTime.UtcNow + timeout;
                while (DateTime.UtcNow < deadline)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (receiver.Available > 0)
                    {
                        var buffer = new byte[4096];
                        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                        var count = receiver.ReceiveFrom(buffer, ref remote);
                        var response = Encoding.UTF8.GetString(buffer, 0, count).Trim('\0');
                        try { results.Add(JsonConvert.DeserializeObject<Broadcast>(response)); }
                        catch (JsonException ex) { _logger?.LogWarning(ex, "Ignoring malformed UDP discovery response"); }
                    }
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
            return results.ToArray();
        }

        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(MachineFactory)); }
        public void Dispose() => _disposed = true;
    }
}
