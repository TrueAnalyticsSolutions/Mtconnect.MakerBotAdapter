using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MakerBot.FastCGI;

namespace MakerBot
{
    /// <summary>
    /// Maintains a Socket connection to a Machine and handles communication via the JSON-RPC protocol.
    /// </summary>
    public class RpcConnection : IDisposable
    {
        private ILogger<RpcConnection> _logger { get; set; }

        /// <summary>
        /// Helps maintain the threads when (dis)connecting with the remote Socket.
        /// </summary>
        private ManualResetEvent asyncTaskSwitch = new ManualResetEvent(false);

        /// <summary>
        /// Reference to the machine's local area address.
        /// </summary>
        public IPEndPoint Endpoint { get; private set; }

        /// <summary>
        /// Socket connection to the machine.
        /// </summary>
        private Socket connection { get; set; } = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// Network stream to help manage Read/Write operations.
        /// </summary>
        private NetworkStream stream { get; set; }
        /// <summary>
        /// Reads inbound messages from the machine's Socket connection.
        /// </summary>
        private StreamReader reader { get; set; }
        /// <summary>
        /// Writes outbound messages to the machine's Socket connection.
        /// </summary>
        private StreamWriter writer { get; set; }

        /// <summary>
        /// Collection of pending unhandled command callbacks.
        /// </summary>
        /// <remarks>The key is a reference to the <see cref="requestId"/> that was used to issue the command and the value is the response from the machine.</remarks>
        private Dictionary<int, JObject> callbacks = new Dictionary<int, JObject>();

        public Dictionary<AccessTokenContexts, string> AccessTokens { get; private set; } = new Dictionary<AccessTokenContexts, string>()
        {
            { AccessTokenContexts.jsonrpc, string.Empty },
            { AccessTokenContexts.put, string.Empty },
            { AccessTokenContexts.camera, string.Empty }
        };

        /// <summary>
        /// Triggered when the network stream finishes parsing a JSON-RPC response from the machine.
        /// </summary>
        public event Action<JObject> OnResponse;

        private int requestId { get; set; }

        private bool IsDisconnectRequested { get; set; } = false;

        public bool IsAuthenticated { get; set; }

        public bool IsConnected
        {
            get
            {
                return connection?.Connected == true;
            }
        }

        public RpcConnection(IPEndPoint endpoint, ILoggerFactory logFactory = default)
        {
            Endpoint = endpoint;
            _logger = logFactory?.CreateLogger<RpcConnection>();
        }
        public RpcConnection(IPAddress address, int port, ILoggerFactory logFactory = default) : this(new IPEndPoint(address, port), logFactory) { }
        public RpcConnection(string host, int port, ILoggerFactory logFactory = default) : this(IPAddress.Parse(host), port, logFactory) { }

        public async Task<bool> Authenticate()
        {
            if (!AccessTokens.ContainsKey(AccessTokenContexts.jsonrpc) || string.IsNullOrEmpty(AccessTokens[AccessTokenContexts.jsonrpc]))
            {
                throw new Exception("You must first get an access token before authenticating.");
            }

            return await Authenticate(AccessTokens[AccessTokenContexts.jsonrpc]);
        }
        /// <summary>
        /// Authenticates this client with the RPC service using the Access Token provided by <see cref="FastCGI"/>.
        /// </summary>
        /// <param name="accessToken">Reference to the access token provided by the <see cref="FastCGI"/> API.</param>
        /// <returns></returns>
        public async Task<bool> Authenticate(string accessToken)
        {
            var result = await SendCommand(new RpcRequest("authenticate", new { access_token = accessToken }));
            IsAuthenticated = result != null && !result.ContainsKey("error");
            return IsAuthenticated;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            if (connection?.Connected == true) return;

            _logger?.LogInformation("Connecting to {Address}...", Endpoint);
            connection.BeginConnect(Endpoint, new AsyncCallback(connectCallback), connection);
            asyncTaskSwitch.WaitOne();
            asyncTaskSwitch.Reset();

            stream = new NetworkStream(connection);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            _logger?.LogInformation("Starting JSON-RPC Listener @{Address}", Endpoint);
            Task.Run(readResponseAsync);
        }

        private void connectCallback(IAsyncResult result)
        {
            try
            {
                connection.EndConnect(result);

                asyncTaskSwitch.Set();
                _logger?.LogInformation("Connected to {Address}...", Endpoint);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect to {Address}", Endpoint);
                throw ex;
            }
        }

        private async Task readResponseAsync()
        {
            const char NULL = '\u0000';
            const char STX = '{';
            const char ETX = '}';

            const int bufferSize = 1024;

            byte[] buffer = new byte[bufferSize];
            int tokenStack = 0;
            StringBuilder responseBuilder = new StringBuilder();
            try
            {
                while (connection.Connected && IsDisconnectRequested == false)
                {
                    // TODO: This should probably wait for a full JSON-RPC formatted message before attempting to parse or trigger the OnResponse event.
                    buffer = new byte[bufferSize];
                    var byteCount = connection.Receive(buffer);
                    if (byteCount <= 0) continue;
                    string packet = Encoding.UTF8.GetString(buffer);
                    if (!string.IsNullOrEmpty(packet)) packet = packet.Trim(NULL).Replace(NULL.ToString(), string.Empty);

                    while (!string.IsNullOrEmpty(packet))
                    {
                        int length = packet.Length;
                        for (int i = 0; i < length; i++)
                        {
                            var c = packet[i];
                            responseBuilder.Append(c);

                            if (c == STX)
                                tokenStack++;
                            if (c == ETX)
                                tokenStack--;

                            if (tokenStack == 0)
                            {
                                string message = responseBuilder.ToString();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    responseBuilder.Clear();
                                    packet = packet.Substring(i+1);
                                    if (!receivedMessage(message))
                                    {
                                        packet = string.Empty;
                                    }
                                    break;
                                }
                            }

                            // Break out of While loop
                            if (i == length - 1)
                                packet = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to read response from {Address} due to error: {Exception}", Endpoint, ex);
            }
        }

        private bool receivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            JObject response = null;
            try
            {
                response = JsonConvert.DeserializeObject<JObject>(message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deserialize response into JSON\r\n\tMessage: {Message}\r\n\tException: {Exception}", message, ex);
                return false;
            }

            if (response == null)
                return false;

            if (response.ContainsKey("id"))
            {
                int id = response["id"].Value<int>();
                if (callbacks.ContainsKey(id))
                {
                    lock (callbacks)
                    {
                        callbacks[id] = response;
                        return true;
                    }
                }
            } else if (response.ContainsKey("method"))
            {
                string method = response["method"].Value<string>();
            } else
            {
                _logger?.LogWarning("Unrecognized RPC response: {Message}", message);
            }

            OnResponse?.Invoke(response);

            return true;
        }

        public Task<JObject> SendCommand(RpcRequest request, CancellationToken cancellationToken = default)
        {
            if (writer == null) throw new NullReferenceException("RPC Service has not started yet");

            int id = ++requestId;
            var message = request.ToJsonRpcRequest(id);
            string json = JsonConvert.SerializeObject(message);
            writer.WriteLine(json);

            lock(callbacks)
            {
                callbacks.Add(id, null);
            }
            return Task.Run<JObject>(() =>
            {
                while (callbacks.ContainsKey(id) || cancellationToken.IsCancellationRequested)
                {
                    if (callbacks.ContainsKey(id))
                    {
                        JObject result = callbacks[id];
                        if (result != null)
                        {
                            callbacks.Remove(id);
                            return result;
                        }
                    }
                }
                return null;
            }, cancellationToken);
        }

        private void disconnectCallback(IAsyncResult result)
        {
            try
            {
                connection.EndDisconnect(result);

                asyncTaskSwitch.Set();
                _logger?.LogInformation("Disconnected from {Address}...", Endpoint);
            }
            catch (SocketException se)
            {
                if (connection.Connected == false)
                {
                    asyncTaskSwitch.Set();
                    _logger?.LogInformation("Disconnected from {Address}...", Endpoint);
                } else
                {
                    _logger?.LogError(se, "Failed to disconnect from {Address}", Endpoint);
                    throw se;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to disconnect from {Address}", Endpoint);
                throw ex;
            }
        }

        public void Stop()
        {
            _logger?.LogInformation("Stopping JSON-RPC Listener @{Address}...", Endpoint);
            IsDisconnectRequested = true;
            connection.BeginDisconnect(true, new AsyncCallback(disconnectCallback), connection);
            asyncTaskSwitch.WaitOne();
            IsDisconnectRequested = false;
            asyncTaskSwitch.Reset();

            stream.Close();
            reader.Close();
            writer.Close();
        }

        public void Dispose()
        {
            Stop();
            connection?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
    }
}
