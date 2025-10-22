using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MakerBot
{
    /// <summary>
    /// Discovers MakerBot machines via UDP broadcast.
    /// Sends {"command":"broadcast"} to port 12307 and listens on the sender's source port (12309).
    /// Broadcasts to global and per-NIC subnet-directed broadcasts to handle multi-NIC setups.
    /// </summary>
    public sealed class MachineFactory : IDisposable
    {
        private readonly ILogger<MachineFactory> _logger;
        private readonly Socket _udp;
        private const int TargetPort = 12307;  // printer listens here
        private const int ReplyPort = 12309;  // we send FROM and receive ON this port

        /// <summary>
        /// Initializes the discovery socket and binds to 0.0.0.0:12309.
        /// </summary>
        public MachineFactory(ILoggerFactory logFactory = default)
        {
            _logger = logFactory?.CreateLogger<MachineFactory>();

            _udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            _udp.Blocking = false;
            _udp.Bind(new IPEndPoint(IPAddress.Any, ReplyPort));

            _logger?.LogDebug("Discovery socket bound to {Local}", _udp.LocalEndPoint);
        }

        /// <summary>
        /// Broadcasts a discovery request and gathers replies for a few seconds.
        /// </summary>
        public Broadcast[] Discover(int seconds = 6)
        {
            var replies = new List<Broadcast>();
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new JObject { ["command"] = "broadcast" }));

            // Build destination list: global broadcast + each NIC's broadcast
            var destinations = new HashSet<IPEndPoint> { new IPEndPoint(IPAddress.Broadcast, TargetPort) };
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null) continue;
                    var bc = ComputeBroadcast(ua.Address, ua.IPv4Mask);
                    if (bc != null) destinations.Add(new IPEndPoint(bc, TargetPort));
                }
            }

            foreach (var ep in destinations)
            {
                try { _udp.SendTo(payload, ep); _logger?.LogDebug("Broadcast → {Dest}", ep); }
                catch (SocketException se) { _logger?.LogDebug(se, "Broadcast failed → {Dest}", ep); }
            }

            var buf = new byte[4096];
            var remote = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(2, seconds));

            while (DateTime.UtcNow < deadline)
            {
                // small poll loop without busy spin
                System.Threading.Thread.Sleep(100);
                try
                {
                    while (_udp.Available > 0)
                    {
                        int len = _udp.ReceiveFrom(buf, ref remote);
                        if (len <= 0) continue;
                        var json = Encoding.UTF8.GetString(buf, 0, len);

                        _logger?.LogTrace("Discovery ← {Remote}: {Json}", remote, json);
                        try
                        {
                            var obj = JsonConvert.DeserializeObject<Broadcast>(json);
                            if (obj != null) replies.Add(obj);
                        }
                        catch { /* ignore non-JSON */ }
                    }
                }
                catch (SocketException se) { _logger?.LogDebug(se, "Receive error on {Local}", _udp.LocalEndPoint); }
            }

            if (replies.Count == 0)
            {
                _logger?.LogWarning("No discovery responses on {Port}. Disable VPN/Tailscale and ensure the printer shares a subnet with one NIC.", ReplyPort);
            }

            return replies.ToArray();
        }

        /// <summary>
        /// Computes a directed broadcast address given an IPv4 address and mask.
        /// </summary>
        private static IPAddress ComputeBroadcast(IPAddress address, IPAddress mask)
        {
            var a = address.GetAddressBytes();
            var m = mask.GetAddressBytes();
            if (a.Length != m.Length) return null;
            var b = new byte[a.Length];
            for (int i = 0; i < a.Length; i++) b[i] = (byte)(a[i] | ~m[i]);
            return new IPAddress(b);
        }

        /// <summary>
        /// Disposes the discovery socket.
        /// </summary>
        public void Dispose()
        {
            try { _udp?.Close(); _udp?.Dispose(); } catch { }
        }
    }
}
