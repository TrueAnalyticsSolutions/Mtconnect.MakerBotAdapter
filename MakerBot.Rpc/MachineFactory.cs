using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class MachineFactory : IDisposable
    {
        private readonly ILogger<MachineFactory> _logger;

        private Dictionary<string, Machine> _machines { get; set; } = new Dictionary<string, Machine>();

        private Socket BroadcastChannel { get; set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket AnswerChannel { get; set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        const int _targetPort = 12307;
        const int _listenPort = 12308;
        const int _sourcePort = 12309;

        public MachineFactory(ILoggerFactory logFactory = default)
        {
            _logger = logFactory?.CreateLogger<MachineFactory>();

            BroadcastChannel.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            BroadcastChannel.Bind(new IPEndPoint(IPAddress.Any, _sourcePort) as EndPoint);

            AnswerChannel.Blocking = false;
            AnswerChannel.Bind(new IPEndPoint(IPAddress.Any, _listenPort));
        }

        public Broadcast[] Discover()
        {
            var broadcastResponses = new List<Broadcast>();

            _logger?.LogInformation("Publishing broadcast command");
            broadcast();

            _logger?.LogInformation("Awaiting broadcast command responses...");
            int maxAttempts = 5;
            int idx = 0;
            byte[] buffer = new byte[1024];
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                System.Threading.Thread.Sleep(1000); // Sleep 1 second
                try
                {
                    if (!AnswerChannel.Blocking && AnswerChannel.Available > 0)
                    {
                        int length = this.AnswerChannel.ReceiveFrom(buffer, ref remoteEndpoint);
                        if (length > 0)
                        {
                            string resp = Encoding.UTF8.GetString(buffer).Trim((char)0x00);
                            var objResponse = JsonConvert.DeserializeObject<Broadcast>(resp);

                            _logger?.LogTrace("Discovery response: {Response}", resp);
                            broadcastResponses.Add(objResponse);
                        }
                    }
                }
                catch (SocketException se)
                {
                    _logger?.LogWarning("Encountered SocketException while attempting to discover machines. Retry attempt {Attempt}/{MaxAttempts}", idx+1, maxAttempts);
                    if (idx == maxAttempts - 1) _logger?.LogError(se, "Failed to discover machines due to SocketException");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to discover machines due to Exception");
                }
                idx++;
            } while (idx < maxAttempts);
            if (idx >= maxAttempts)
            {
                //write("\tTimed Out!", ConsoleColor.Red);
            }

            return broadcastResponses.ToArray();
        }

        private void broadcast(int timeoutMilliseconds = 30_000)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentException(nameof(timeoutMilliseconds), "Timeout must be greater than zero");

            JObject broadcast = new JObject();
            broadcast["command"] = "broadcast";

            byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(broadcast));
            var endpoint = new IPEndPoint(IPAddress.Broadcast, _targetPort);
            _logger?.LogDebug("Broadcasting to endpoint: {Endpoint}", endpoint.ToString());
            BroadcastChannel.SendTo(payload, endpoint);
            return;
        }

        public void Dispose()
        {
            _machines.Clear();
            BroadcastChannel?.Dispose();
            AnswerChannel?.Dispose();
        }
    }
}
