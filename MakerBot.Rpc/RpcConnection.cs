using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot
{
    /// <summary>
    /// Maintains a TCP JSON-RPC 2.0 connection to a MakerBot and provides async request/response APIs.
    /// Messages are newline-delimited JSON (one object per line).
    /// </summary>
    public sealed class RpcConnection : IDisposable
    {
        private readonly ILogger<RpcConnection> _logger;
        private readonly string _host;
        private readonly int _port;

        private TcpClient _tcp;
        private NetworkStream _net;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _pumpCts;

        private int _nextId = 0;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pending = new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();

        /// <summary>
        /// Fired for notifications (messages with "method" and no "id").
        /// </summary>
        public event Action<JObject> OnNotification;

        /// <summary>
        /// Indicates if the underlying TCP socket is connected.
        /// </summary>
        public bool IsConnected => _tcp?.Connected == true;

        /// <summary>
        /// Creates a new RPC connection wrapper.
        /// </summary>
        public RpcConnection(string host, int port = 9999, ILoggerFactory logFactory = default)
        {
            _host = host;
            _port = port;
            _logger = logFactory?.CreateLogger<RpcConnection>();
        }

        /// <summary>
        /// Opens the TCP connection and starts the receive loop.
        /// Optionally sends an authenticate call if a token is provided.
        /// </summary>
        public async Task ConnectAsync(string accessToken = null, CancellationToken ct = default)
        {
            _tcp = new TcpClient();
            await _tcp.ConnectAsync(_host, _port);
            _net = _tcp.GetStream();
            _reader = new StreamReader(_net, Encoding.UTF8);
            _writer = new StreamWriter(_net, Encoding.UTF8) { AutoFlush = true };

            _pumpCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceivePumpAsync(_pumpCts.Token));

            _logger?.LogInformation("Connected to MakerBot RPC {Host}:{Port}", _host, _port);

            if (!string.IsNullOrEmpty(accessToken))
            {
                var auth = await SendAsync("authenticate", new { access_token = accessToken }, timeoutMs: 5000, ct);
                if (auth?["error"] != null)
                    throw new UnauthorizedAccessException(auth["error"]?.ToString());
            }
        }

        /// <summary>
        /// Sends a JSON-RPC request and awaits the response (with timeout).
        /// </summary>
        public async Task<JObject> SendAsync(string method, object @params = null, int timeoutMs = 5000, CancellationToken ct = default)
        {
            var id = Interlocked.Increment(ref _nextId);
            var req = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["id"] = id
            };
            if (@params != null) req["params"] = JObject.FromObject(@params);

            var tcs = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;

            string line = JsonConvert.SerializeObject(req);
            await _writer.WriteLineAsync(line);
            _logger?.LogTrace("→ {Json}", line);

            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeoutCts.CancelAfter(Math.Max(1000, timeoutMs));

                using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    try { return await tcs.Task.ConfigureAwait(false); }
                    catch (TaskCanceledException) { _pending.TryRemove(id, out _); throw new TimeoutException($"RPC '{method}' timed out."); }
                }
            }
        }

        /// <summary>
        /// Gracefully closes the TCP connection.
        /// </summary>
        public void Close()
        {
            try { _pumpCts?.Cancel(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _net?.Dispose(); } catch { }
            try { _tcp?.Dispose(); } catch { }

            foreach (var kv in _pending)
                kv.Value.TrySetCanceled();

            _pending.Clear();
        }

        /// <summary>
        /// Disposes the connection and resources.
        /// </summary>
        public void Dispose() => Close();

        /// <summary>
        /// Background receive loop parsing newline-delimited JSON objects.
        /// Routes responses by id and raises notifications for method-only messages.
        /// </summary>
        private async Task ReceivePumpAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null) break; // remote closed

                    _logger?.LogTrace("← {Json}", line);

                    JObject msg = null;
                    try { msg = JObject.Parse(line); }
                    catch (Exception ex) { _logger?.LogDebug(ex, "Invalid JSON received"); continue; }

                    // Response with id
                    if (msg.TryGetValue("id", out var idTok) && idTok.Type == JTokenType.Integer)
                    {
                        var id = idTok.Value<int>();
                        if (_pending.TryRemove(id, out var tcs))
                            tcs.TrySetResult(msg);
                        continue;
                    }

                    // Notification (no id)
                    if (msg.TryGetValue("method", out _))
                    {
                        OnNotification?.Invoke(msg);
                        continue;
                    }

                    _logger?.LogDebug("Unrecognized message: {Msg}", msg.ToString(Formatting.None));
                }
            }
            catch (IOException) { /* connection dropped */ }
            catch (ObjectDisposedException) { /* shutting down */ }
            catch (Exception ex) { _logger?.LogError(ex, "Receive loop failed"); }
            finally
            {
                // fail any waiting callers
                foreach (var kv in _pending)
                    kv.Value.TrySetException(new IOException("Connection closed"));
                _pending.Clear();
            }
        }
    }
}
