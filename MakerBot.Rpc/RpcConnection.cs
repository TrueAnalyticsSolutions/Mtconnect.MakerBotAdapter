using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MakerBot.FastCGI;

namespace MakerBot
{
    /// <summary>Maintains a newline-delimited JSON-RPC connection to a MakerBot.</summary>
    public sealed class RpcConnection : IDisposable
    {
        private readonly ILogger<RpcConnection> _logger;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pending = new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly object _lifecycleLock = new object();
        private Socket _socket;
        private NetworkStream _stream;
        private CancellationTokenSource _lifetime;
        private Task _readerTask;
        private int _requestId;
        private bool _disposed;

        public event EventHandler ConnectionChanged;
        public event EventHandler Notification;
        public event Action<JObject> OnResponse;

        public IPEndPoint Endpoint { get; }
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public ConcurrentDictionary<AccessTokenContexts, string> AccessTokens { get; } = new ConcurrentDictionary<AccessTokenContexts, string>();
        public bool IsAuthenticated { get; private set; }
        public bool IsConnected => _socket != null && _socket.Connected && _lifetime != null && !_lifetime.IsCancellationRequested;

        public RpcConnection(IPEndPoint endpoint, ILoggerFactory logFactory = null)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _logger = logFactory?.CreateLogger<RpcConnection>();
        }

        public RpcConnection(IPAddress address, int port, ILoggerFactory logFactory = null) : this(new IPEndPoint(address, port), logFactory) { }
        public RpcConnection(string host, int port, ILoggerFactory logFactory = null) : this(Resolve(host, port), logFactory) { }

        private static IPEndPoint Resolve(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required.", nameof(host));
            if (IPAddress.TryParse(host, out var address)) return new IPEndPoint(address, port);
            foreach (var candidate in Dns.GetHostAddresses(host).OrderBy(x => x.AddressFamily == AddressFamily.InterNetwork ? 0 : 1))
                if (candidate.AddressFamily == AddressFamily.InterNetwork || candidate.AddressFamily == AddressFamily.InterNetworkV6)
                    return new IPEndPoint(candidate, port);
            throw new SocketException((int)SocketError.HostNotFound);
        }

        public void Start(CancellationToken cancellationToken = default) => StartAsync(cancellationToken).GetAwaiter().GetResult();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            lock (_lifecycleLock)
            {
                if (IsConnected) return;
                _socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            _logger?.LogInformation("Connecting to {Address}...", Endpoint);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(ConnectTimeout);
                var connectTask = Task.Factory.FromAsync(
                    (callback, state) => _socket.BeginConnect(Endpoint, callback, state),
                    _socket.EndConnect,
                    null);
                var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, timeout.Token)).ConfigureAwait(false);
                if (completed != connectTask)
                {
                    Stop();
                    timeout.Token.ThrowIfCancellationRequested();
                }
                await connectTask.ConfigureAwait(false);
            }

            _stream = new NetworkStream(_socket, ownsSocket: false);
            _readerTask = ReadResponsesAsync(_lifetime.Token);
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            _logger?.LogInformation("Connected to {Address}", Endpoint);
        }

        public async Task<bool> Authenticate(CancellationToken cancellationToken = default)
        {
            if (!AccessTokens.TryGetValue(AccessTokenContexts.jsonrpc, out var token) || string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("A JSON-RPC access token is required before authenticating.");
            return await Authenticate(token, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> Authenticate(string accessToken, CancellationToken cancellationToken = default)
        {
            var result = await SendCommand(new RpcRequest("authenticate", new { access_token = accessToken }), cancellationToken).ConfigureAwait(false);
            IsAuthenticated = result != null && !result.ContainsKey("error");
            return IsAuthenticated;
        }

        public async Task<JObject> SendCommand(RpcRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!IsConnected || _stream == null) throw new InvalidOperationException("RPC service has not started.");

            var id = Interlocked.Increment(ref _requestId);
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(id, completion)) throw new InvalidOperationException("Duplicate JSON-RPC request id.");

            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token))
            using (timeout.Token.Register(() => completion.TrySetCanceled()))
            {
                timeout.CancelAfter(RequestTimeout);
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request.ToJsonRpcRequest(id)) + "\n");
                try
                {
                    await _writeLock.WaitAsync(timeout.Token).ConfigureAwait(false);
                    try
                    {
                        await _stream.WriteAsync(payload, 0, payload.Length, timeout.Token).ConfigureAwait(false);
                        await _stream.FlushAsync(timeout.Token).ConfigureAwait(false);
                    }
                    finally { _writeLock.Release(); }
                    return await completion.Task.ConfigureAwait(false);
                }
                finally { _pending.TryRemove(id, out _); }
            }
        }

        private async Task ReadResponsesAsync(CancellationToken cancellationToken)
        {
            var bytes = new byte[4096];
            var buffer = new StringBuilder();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var count = await _stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                    if (count == 0) break;
                    buffer.Append(Encoding.UTF8.GetString(bytes, 0, count).Replace("\0", string.Empty));
                    while (TryTakeJson(buffer, out var json)) HandleMessage(json);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger?.LogWarning(ex, "JSON-RPC reader stopped for {Address}", Endpoint); }
            finally
            {
                IsAuthenticated = false;
                foreach (var pending in _pending.Values)
                    pending.TrySetException(new System.IO.IOException("The MakerBot JSON-RPC connection closed."));
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal static bool TryTakeJson(StringBuilder buffer, out string json)
        {
            json = null;
            var start = -1;
            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var i = 0; i < buffer.Length; i++)
            {
                var c = buffer[i];
                if (start < 0)
                {
                    if (c != '{') continue;
                    start = i;
                    depth = 1;
                    continue;
                }
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0)
                {
                    json = buffer.ToString(start, i - start + 1);
                    buffer.Remove(0, i + 1);
                    return true;
                }
            }
            if (start > 0) buffer.Remove(0, start);
            return false;
        }

        private void HandleMessage(string message)
        {
            JObject response;
            try { response = JObject.Parse(message); }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Ignoring malformed JSON-RPC message");
                return;
            }

            if (response["id"] != null && _pending.TryGetValue(response["id"].Value<int>(), out var completion))
                completion.TrySetResult(response);
            else
            {
                Notification?.Invoke(this, EventArgs.Empty);
                OnResponse?.Invoke(response);
            }
        }

        public void Stop()
        {
            lock (_lifecycleLock)
            {
                if (_lifetime == null) return;
                _lifetime.Cancel();
                try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
                try { _stream?.Dispose(); } catch { }
                try { _socket?.Dispose(); } catch { }
                _stream = null;
                _socket = null;
                _lifetime.Dispose();
                _lifetime = null;
                IsAuthenticated = false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcConnection));
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _writeLock.Dispose();
            _disposed = true;
        }
    }
}
