using MakerBot;
using MakerBot.Rpc;
using Mtconnect.AdapterSdk;
using Mtconnect.AdapterSdk.DataItemValues;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mtconnect.MakerBotAdapter
{
    public sealed class MakerBotRPCAdapter : IAdapterSource, IDisposable
    {
        private readonly IAdapterLogger _logger;
        private readonly AdapterConfiguration _config;
        private readonly SemaphoreSlim _cycleLock = new SemaphoreSlim(1, 1);
        private readonly System.Timers.Timer _timer;
        private CancellationTokenSource _lifetime;
        private DateTime _nextReconnectUtc;
        private bool _disposed;
        private bool _started;
        private bool _stopped;
        private MakerBotMachine _model = new MakerBotMachine();
        private readonly ConditionState _alarmState = new ConditionState();
        private readonly ConditionState[] _toolErrorStates = { new ConditionState(), new ConditionState() };

        public event DataReceivedHandler OnDataReceived;
        public event AdapterSourceStartedHandler OnAdapterSourceStarted;
        public event AdapterSourceStoppedHandler OnAdapterSourceStopped;

        public Machine Machine { get; private set; }
        public string DeviceUuid => CreateUuid(_config.Machine.SerialNumber);
        public string DeviceName => _config.Machine.Name;
        public string StationId => _config.Machine.SerialNumber;
        public string SerialNumber => _config.Machine.SerialNumber;
        public string Manufacturer => "MakerBot";
        internal MakerBotMachine CurrentModel => _model;

        public MakerBotRPCAdapter(string configPath, IAdapterLogger logger = null)
        {
            _logger = logger;
            _config = LoadConfiguration(configPath);
            _timer = new System.Timers.Timer(_config.PollIntervalMilliseconds) { AutoReset = true };
            _timer.Elapsed += async (sender, args) => await PollAsync().ConfigureAwait(false);
        }

        public void Start(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (_started && !_stopped) return;
            _lifetime?.Dispose();
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            _stopped = false;
            SetUnavailable();
            try
            {
                ConnectAsync(_lifetime.Token).GetAwaiter().GetResult();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _nextReconnectUtc = DateTime.UtcNow.AddMilliseconds(_config.ReconnectIntervalMilliseconds);
                _logger?.LogWarning(ex, "Initial MakerBot connection failed; the adapter will retry");
            }
            _timer.Start();
            _started = true;
            OnAdapterSourceStarted?.Invoke(this, new AdapterSourceStartedEventArgs());
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            Exception directFailure = null;
            try
            {
                await ReplaceMachineAsync(Clone(_config.Machine), cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                directFailure = ex;
                _logger?.LogWarning(ex, "Configured MakerBot endpoint {0}:{1} was unavailable; discovering serial {2}", _config.Machine.Address, _config.Machine.RpcPort, _config.Machine.SerialNumber);
            }

            if (directFailure is UnauthorizedAccessException) throw directFailure;

            using (var factory = new MachineFactory())
            {
                var discovered = await factory.DiscoverBySerialAsync(_config.Machine.SerialNumber, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (discovered == null) throw new IOException("Could not connect to or rediscover the configured MakerBot.", directFailure);
                var discoveredConfig = Clone(_config.Machine);
                discoveredConfig.Address = discovered.ip;
                discoveredConfig.RpcPort = int.TryParse(discovered.port, out var port) ? port : 9999;
                await ReplaceMachineAsync(discoveredConfig, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReplaceMachineAsync(MakerBot.MachineConfig config, CancellationToken cancellationToken)
        {
            var candidate = new Machine(config);
            try
            {
                candidate.Connection.ConnectionChanged += ConnectionChanged;
                candidate.Connection.OnResponse += ProcessPayload;
                await candidate.StartAsync(false, cancellationToken).ConfigureAwait(false);
                Machine?.Dispose();
                Machine = candidate;
                _config.Machine.Address = candidate.Config.Address;
                _config.Machine.RpcPort = candidate.Config.RpcPort;
                _config.Machine.Name = candidate.Config.Name;
                SetAvailable();
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
        }

        private void ConnectionChanged(object sender, EventArgs args)
        {
            var connection = sender as RpcConnection;
            _model.ConnectionStatus = connection?.IsConnected == true
                ? ConnectionStatus.ESTABLISHED
                : ConnectionStatus.CLOSED;
            Publish();
        }

        private async Task PollAsync()
        {
            if (_lifetime == null || _lifetime.IsCancellationRequested || !await _cycleLock.WaitAsync(0).ConfigureAwait(false)) return;
            try
            {
                if (Machine?.Connection?.IsConnected != true || Machine.Connection.IsAuthenticated != true)
                {
                    SetUnavailable();
                    if (DateTime.UtcNow >= _nextReconnectUtc)
                    {
                        _nextReconnectUtc = DateTime.UtcNow.AddMilliseconds(_config.ReconnectIntervalMilliseconds);
                        try { await ConnectAsync(_lifetime.Token).ConfigureAwait(false); }
                        catch (Exception ex) { _logger?.LogWarning(ex, "MakerBot reconnect failed"); }
                    }
                    return;
                }

                var response = await Machine.Connection.GetSystemInformation(_lifetime.Token).ConfigureAwait(false);
                ProcessPayload(response);
                try { ProcessToolOffset((float)await Machine.Connection.GetZAdjustedOffset().ConfigureAwait(false)); }
                catch (Exception ex) { _logger?.LogDebug("Tool offset request failed: {0}", ex.Message); }
                Publish();
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "MakerBot polling failed");
                SetUnavailable();
            }
            finally { _cycleLock.Release(); }
        }

        internal void ProcessPayload(JObject payload)
        {
            if (payload == null) return;
            if (payload["error"] != null)
            {
                SetAlarmFault(payload["error"], "RPC_ERROR", "MakerBot RPC error");
                Publish();
                return;
            }

            var info = payload["result"] ?? payload["params"]?["info"];
            if (!(info is JObject infoObject)) return;
            if (infoObject["toolheads"] == null && infoObject["current_process"] == null &&
                infoObject["ip"] == null && infoObject["machine_name"] == null) return;
            info = infoObject;
            _model.ConnectionStatus = "ESTABLISHED";
            _model.Availability = "AVAILABLE";
            _model.IPv4 = info.Value<string>("ip") ?? Machine?.Config.Address;
            _model.Port = Machine?.Config.RpcPort ?? 0;
            var extruders = info["toolheads"]?["extruder"] as JArray;
            if (extruders != null)
            {
                foreach (var extruder in extruders)
                {
                    var index = extruder.Value<int?>("index") ?? 0;
                    var target = index == 0 ? _model.Extruder1 : _model.Extruder2;
                    target.CurrentTemperature = extruder.Value<int?>("current_temperature");
                    target.TargetTemperature = extruder.Value<int?>("target_temperature");
                    var toolId = extruder.Value<int?>("tool_id");
                    target.ToolNumber = toolId?.ToString(CultureInfo.InvariantCulture) ?? AdapterSdk.Constants.UNAVAILABLE;
                    if (toolId.HasValue && MakerBotCatalog.TryGetTool(toolId.Value, out var tool))
                    {
                        target.ToolType = tool.Type;
                        target.ToolName = tool.Name;
                        target.ToolMaterial = tool.DefaultMaterialName;
                    }
                    else
                    {
                        target.ToolType = AdapterSdk.Constants.UNAVAILABLE;
                        target.ToolName = AdapterSdk.Constants.UNAVAILABLE;
                        target.ToolMaterial = AdapterSdk.Constants.UNAVAILABLE;
                    }

                    var toolError = extruder.Value<int?>("error") ?? 0;
                    if (toolError == 0)
                    {
                        SetConditionNormal(target.ToolError, _toolErrorStates[index == 0 ? 0 : 1]);
                    } else
                    {
                        var nativeCode = toolError.ToString(CultureInfo.InvariantCulture);
                        SetConditionFault(
                            target.ToolError,
                            _toolErrorStates[index == 0 ? 0 : 1],
                            nativeCode,
                            MakerBotCatalog.GetToolheadErrorName(toolError) ?? $"Unknown MakerBot toolhead error {nativeCode}");
                    }
                }
            }
            var process = info["current_process"];
            if (process == null || process.Type == JTokenType.Null)
            {
                _model.Program = "UNAVAILABLE";
                _model.Execution = "READY";
                _model.ProcessOccurrenceId = AdapterSdk.Constants.UNAVAILABLE;
                _model.ProcessTimer = new AdapterSdk.DataItemValues.ProcessTimer.Process(AdapterSdk.Constants.UNAVAILABLE);
                SetConditionNormal(_model.Alarm, _alarmState);
            }
            else
            {
                _model.Program = process.Value<string>("name") ?? process.Value<string>("filepath") ?? process.Value<string>("filename") ?? "UNAVAILABLE";
                _model.Execution = process.Value<bool?>("cancelled") == true ? "INTERRUPTED" : process.Value<bool?>("complete") == true ? "PROGRAM_COMPLETED" : "ACTIVE";
                _model.ProcessOccurrenceId = process["id"]?.ToString();
                // MakerBot reports elapsed_time in seconds. MakerBot Print divides this value by 60 when displaying minutes.
                _model.ProcessTimer = new AdapterSdk.DataItemValues.ProcessTimer.Process(process.Value<float?>("elapsed_time"));
                var processError = process["error"];
                if (processError == null || processError.Type == JTokenType.Null)
                {
                    SetConditionNormal(_model.Alarm, _alarmState);
                } else
                {
                    SetAlarmFault(processError, "PROCESS_ERROR", "MakerBot process error");
                }
            }
            Publish();
        }

        // MakerBot Print presents GetZAdjustedOffset in millimeters (normally -0.8 mm through +0.8 mm).
        internal void ProcessToolOffset(float? offset) => _model.ToolOffset = new AdapterSdk.DataItemValues.ToolOffset.Length(offset);

        private void SetAlarmFault(JToken error, string fallbackCode, string fallbackMessage)
        {
            var nativeCode = error?["code"]?.ToString();
            if (string.IsNullOrWhiteSpace(nativeCode)) nativeCode = error?["error_id"]?.ToString();
            if (string.IsNullOrWhiteSpace(nativeCode)) nativeCode = fallbackCode;

            var catalogName = int.TryParse(nativeCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCode)
                ? MakerBotCatalog.GetMachineErrorName(numericCode)
                : null;
            var nativeMessage = error?["message"]?.ToString();
            var message = !string.IsNullOrWhiteSpace(catalogName) && !string.IsNullOrWhiteSpace(nativeMessage)
                ? $"{catalogName}: {nativeMessage}"
                : catalogName ?? nativeMessage ?? fallbackMessage;

            SetConditionFault(_model.Alarm, _alarmState, nativeCode, message);
        }

        private static void SetConditionNormal(AdapterSdk.DataItems.Condition condition, ConditionState state)
        {
            if (!state.HasObservation)
            {
                condition.SetNormal();
                state.SetNormal();
                return;
            }

            if (state.NativeCode == null) return;

            condition[state.NativeCode].Normal();
            state.SetNormal();
        }

        private static void SetConditionFault(
            AdapterSdk.DataItems.Condition condition,
            ConditionState state,
            string nativeCode,
            string message)
        {
            if (state.HasObservation &&
                string.Equals(state.NativeCode, nativeCode, StringComparison.Ordinal) &&
                string.Equals(state.Message, message, StringComparison.Ordinal)) return;

            if (state.NativeCode != null && !string.Equals(state.NativeCode, nativeCode, StringComparison.Ordinal))
            {
                condition[state.NativeCode].Normal();
            }

            condition[nativeCode].Fault(message);
            state.SetFault(nativeCode, message);
        }

        private void SetAvailable()
        {
            _model.ConnectionStatus = ConnectionStatus.ESTABLISHED;
            _model.Availability = Availability.AVAILABLE;
            _model.IPv4 = Machine.Config.Address;
            _model.Port = Machine.Config.RpcPort;
            Publish();
        }

        internal void SetUnavailable()
        {
            if (_model.Availability?.Value?.ToString() == "UNAVAILABLE" && _model.ConnectionStatus?.Value?.ToString() == "CLOSED") return;
            _model.ConnectionStatus = ConnectionStatus.CLOSED;
            _model.Availability = Availability.UNAVAILABLE;
            Publish();
        }

        private void Publish() => OnDataReceived?.Invoke(this, new DataReceivedEventArgs(_model));

        private sealed class ConditionState
        {
            public bool HasObservation { get; private set; }
            public string NativeCode { get; private set; }
            public string Message { get; private set; }

            public void SetNormal()
            {
                HasObservation = true;
                NativeCode = null;
                Message = null;
            }

            public void SetFault(string nativeCode, string message)
            {
                HasObservation = true;
                NativeCode = nativeCode;
                Message = message;
            }
        }

        public void Stop(Exception ex = null)
        {
            if (!_started || _stopped) return;
            _stopped = true;
            _timer.Stop();
            _lifetime?.Cancel();
            Machine?.Stop();
            OnAdapterSourceStopped?.Invoke(this, new AdapterSourceStoppedEventArgs(ex));
        }

        public AdapterSourceDescriptor GetSourceDescriptor() => AdapterDescriptorFactory.CreateSourceDescriptor(this, new[] { typeof(MakerBotMachine) });

        private static AdapterConfiguration LoadConfiguration(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("A path to adapterconfig.json is required.", nameof(configPath));
            var fullPath = Path.GetFullPath(configPath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("MakerBot adapter configuration was not found.", fullPath);
            AdapterConfiguration config;
            try
            {
                config = JsonSerializer.Deserialize<AdapterConfiguration>(File.ReadAllText(fullPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip });
            }
            catch (JsonException ex) { throw new InvalidDataException("MakerBot adapter configuration is not valid JSON.", ex); }
            if (config?.Machine == null) throw new InvalidDataException("Configuration property 'machine' is required.");
            if (string.IsNullOrWhiteSpace(config.Machine.Name)) throw new InvalidDataException("Configuration property 'machine.name' is required.");
            if (string.IsNullOrWhiteSpace(config.Machine.SerialNumber)) throw new InvalidDataException("Configuration property 'machine.serialNumber' is required.");
            if (string.IsNullOrWhiteSpace(config.Machine.Address)) throw new InvalidDataException("Configuration property 'machine.address' is required.");
            if (config.Machine.RpcPort < 1 || config.Machine.RpcPort > 65535) throw new InvalidDataException("machine.rpcPort must be between 1 and 65535.");
            if (config.Machine.SslPort < 1 || config.Machine.SslPort > 65535) throw new InvalidDataException("machine.sslPort must be between 1 and 65535.");
            if (string.IsNullOrWhiteSpace(config.Machine.AuthenticationCode)) throw new MakerBotPairingRequiredException("Configuration property 'machine.authenticationCode' is missing. Run MakerBot.Configurator.");
            if (config.PollIntervalMilliseconds < 200) throw new InvalidDataException("pollIntervalMilliseconds must be at least 200.");
            if (config.ReconnectIntervalMilliseconds < 1000) throw new InvalidDataException("reconnectIntervalMilliseconds must be at least 1000.");
            return config;
        }

        private static MakerBot.MachineConfig Clone(MakerBot.MachineConfig value) => new MakerBot.MachineConfig { Name = value.Name, SerialNumber = value.SerialNumber, Address = value.Address, RpcPort = value.RpcPort, SslPort = value.SslPort, AuthenticationCode = value.AuthenticationCode, ClientId = value.ClientId, ClientSecret = value.ClientSecret };
        private static string CreateUuid(string serial)
        {
            using (var md5 = MD5.Create()) return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes("MakerBot:" + serial))).ToString();
        }
        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(MakerBotRPCAdapter)); }
        public void Dispose() { if (_disposed) return; Stop(); Machine?.Dispose(); _timer.Dispose(); _cycleLock.Dispose(); _lifetime?.Dispose(); _disposed = true; }
    }

}
