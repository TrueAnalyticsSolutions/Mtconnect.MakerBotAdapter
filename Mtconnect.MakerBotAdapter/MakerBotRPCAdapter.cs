using MakerBot;
using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Mtconnect.AdapterSdk.DataItemValues;
using Mtconnect.MakerBotAdapter.Lookups;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static MakerBot.Rpc.SystemInformation.Result.Toolheads;
using static System.Net.Mime.MediaTypeNames;

namespace Mtconnect.MakerBotAdapter
{
    public class MakerBotRPCAdapter : IAdapterSource, IDisposable
    {
        public const int MAX_RECONNECT_ATTEMPTS = 5;
        private int _reconnectAttempts;

        private string uuid = null;
        public string DeviceUuid => uuid ?? (uuid = ToUuid(SerialNumber ?? DeviceName));

        public string DeviceName => Machine?.Config?.Name;

        public string StationId =>Environment.MachineName;

        public string SerialNumber => Machine?.Config?.Serial;

        public string Manufacturer => "MakerBot Industries, LLC";

        private ILoggerFactory _loggerFactory;
        private ILogger _logger { get; set; }
        
        public event DataReceivedHandler OnDataReceived;
        public event AdapterSourceStartedHandler OnAdapterSourceStarted;
        public event AdapterSourceStoppedHandler OnAdapterSourceStopped;

        private bool _busy { get; set; }

        private System.Timers.Timer _timer { get; set; } = new System.Timers.Timer();

        public MakerBot.Machine Machine { get; set; } = null;


        private string _serialNumber { get; set; }
        private string _authCode { get; set; }
        private MakerBotMachine _model { get; set; } = new MakerBotMachine();

        /// <summary>
        /// Constructs a new instance of the MTConnect Adapter for MakerBot (Gen 5+) machine using the onboard JSON-RPC service.
        /// </summary>
        /// <param name="serialNumber">Reference to the machine's serial number. Used as a lookup when discovering machines via broadcast discovery.</param>
        /// <param name="authCode">Copy of the authentication code used to connect the machine. You may need to use a separate application to obtain an Auth code.</param>
        /// <param name="pollRate">The rate at which the adapter will poll the RPC service for more data.</param>
        /// <param name="loggerFactory"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public MakerBotRPCAdapter(string serialNumber, string authCode = null, double pollRate = 5_000, ILoggerFactory loggerFactory = default)
        {
            if (pollRate <= 0) throw new IndexOutOfRangeException("Poll rate cannot be less than or equal to zero");
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<MakerBotRPCAdapter>();

            _serialNumber = serialNumber;
            _authCode = authCode;

            _timer.Interval = pollRate;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_busy) return;
            _busy = true;
            if (!Machine.Connection.IsConnected)
            {
                _logger?.LogWarning("Machine has disconnected");
                _model.Availability = Availability.UNAVAILABLE;

                // Attempt to reconnect
                try
                {
                    Machine.Connection.Start();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to reconnect to machine");
                    _reconnectAttempts++;
                    if (_reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
                    {
                        _busy = false;
                        var maxAttemptException = new Exception("Failed too many reconnection attempts", ex);
                        Stop(maxAttemptException);
                        return;
                    }
                }
                _busy = false;
                return;
            } else if (!Machine.Connection.IsAuthenticated)
            {
                // TODO: Re-Authorize, but may be unnecessary because Authorization/Authentication is handled in the start call
            }

            _model.Controller.Path.ToolOffset = Convert.ToInt32(Machine.Connection.GetZAdjustedOffset().Result);

            // Let system notification update the current toolId, let's use that as a lookup
            var machineConfig = Machine.MachineConfigResponse;
            if (machineConfig != null)
            {
                string modelId = _model.Auxiliaries?.ModelExtruder?.ExtruderId?.ToString();
                if (!string.IsNullOrEmpty(modelId))
                {
                    string modelType = Machine.MachineConfigResponse?.extruder_profiles?.supported_extruders?[modelId]?.ToString();
                    if (!string.IsNullOrEmpty(modelType))
                    {
                        string modelName = Extruders.GetNameByType(modelType);
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            _model.Auxiliaries.ModelExtruder.Extruder = modelName;
                        }

                        var materialType = machineConfig.extruder_profiles.extruder_profiles[modelType]?.materials?.FirstOrDefault().Key;
                        if (!string.IsNullOrEmpty(materialType))
                        {
                            _model.Auxiliaries.ModelExtruder.FilamentType = Materials.GetNameByType(materialType);
                        }
                    }
                }

                string supportId = _model.Auxiliaries?.SupportExtruder?.ExtruderId?.ToString();
                if (!string.IsNullOrEmpty(supportId))
                {
                    string supportType = Machine.MachineConfigResponse?.extruder_profiles?.supported_extruders?[supportId]?.ToString();
                    if (!string.IsNullOrEmpty(supportType))
                    {
                        string supportName = Extruders.GetNameByType(supportType);
                        if (!string.IsNullOrEmpty(supportName))
                        {
                            _model.Auxiliaries.SupportExtruder.Extruder = supportName;
                        }

                        var materialType = machineConfig.extruder_profiles.extruder_profiles[supportType]?.materials?.FirstOrDefault().Key;
                        if (!string.IsNullOrEmpty(materialType))
                        {
                            _model.Auxiliaries.SupportExtruder.FilamentType = Materials.GetNameByType(materialType);
                        }
                    }
                }
            }

            _busy = false;

            OnDataReceived?.Invoke(this, new DataReceivedEventArgs(_model));
        }

        public void Start(CancellationToken token = default)
        {

            using (var machineFactory = new MachineFactory())
            {
                var discoveries = machineFactory.Discover();
                if (discoveries != null)
                {
                    var match = discoveries.FirstOrDefault(o => o.iserial == _serialNumber);
                    if (match != null)
                    {
                        Machine = new MakerBot.Machine(IPAddress.Parse(match.ip), int.Parse(match.port), _loggerFactory);
                        Machine.Config.Address = match.ip;
                        Machine.Config.Port = int.Parse(match.port);

                        if (!string.IsNullOrEmpty(_authCode))
                        {
                            _logger?.LogInformation("Using Authorization Code: {AuthCode}", _authCode);
                            Machine.Config.AuthenticationCode = _authCode;
                        }
                        else
                        {
                            _logger?.LogWarning("Recommended to provide an authCode for the RPC connection");
                        }
                        Machine.Connection.OnResponse += Connection_OnResponse;
                        Machine.Config.Serial = match.iserial;
                        Machine.Config.Name = match.machine_name;
                        Machine.SSL = int.Parse(match.ssl_port);
                        Machine.VID = match.vid.ToString();
                        Machine.ApiVersion = match.api_version;
                        Machine.MotorDriveVersion = match.motor_driver_version;
                        Machine.BotType = match.bot_type;
                        Machine.FirmwareVersion = match.firmware_version.ToString();
                        Machine.MachineType = match.machine_type;
                        Machine.PID = match.pid;

                        _model.Port = int.Parse(match.port);
                        _model.IPv4 = match.ip;
                        _logger?.LogDebug("Found machine through network discovery");
                    } else
                    {
                        _logger?.LogWarning("Couldn't discover machine in broadcast");
                    }
                } else
                {
                    _logger?.LogWarning("No machines discovered on the network");
                }
            }
            if (Machine == null)
            {
                var missingMachine = new Exception(("Machine was not initialized"));
                _logger?.LogWarning(missingMachine, missingMachine.Message);
                Stop();
                throw missingMachine;
            }
            Machine.Start(token);

            // TODO: Get more machine information to fill in config
            var sysInfo = Machine.Connection.GetSystemInformation().Result;
            if (sysInfo != null)
            {
                _logger?.LogDebug("Received SystemInformation: {@SysInfo}", sysInfo);
            } else
            {
                _logger?.LogDebug("Could not retrieve SystemInformation");
            }


            _timer.Start();

            OnAdapterSourceStarted?.Invoke(this, new AdapterSourceStartedEventArgs());
        }

        private void Connection_OnResponse(Newtonsoft.Json.Linq.JObject obj)
        {
            _model.Availability = Availability.AVAILABLE;

            if (obj.ContainsKey("method"))
            {
                string method = obj["method"].ToString();
                switch (method)
                {
                    case "system_notification":
                        SystemNotification system_notification = null;
                        try
                        {
                            system_notification = obj.ToObject<SystemNotification>();
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Could not parse 'system_notification': {@Result}\r\n{@Exception}", obj, ex);
                            break;
                        }
                        if (system_notification == null)
                            break;

                        if (system_notification?.@params == null)
                        {
                            _logger?.LogWarning("Could not parse 'system_notification': {@Result}", obj);
                        }

                        ProcessSystemNotification(system_notification);
                        break;
                    case "state_notification":
                        SystemNotification state_notification = null;
                        try
                        {
                            state_notification = obj.ToObject<SystemNotification>();
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Could not parse 'state_notification': {@Result}\r\n{@Exception}", obj, ex);
                            break;
                        }
                        if (state_notification == null)
                            break;

                        if (state_notification?.@params == null)
                        {
                            _logger?.LogWarning("Could not parse 'system_notification': {@Result}", obj);
                        }

                        ProcessSystemNotification(state_notification);
                        break;
                    case "extruder_change":
                        // TODO: Trigger an Asset_Changed event in the adapter.
                        /*
                         * "jsonrpc": "2.0", "method": "extruder_change", "params": {
                              "config": {
                                "calibrated": true,
                                "id": 99
                              },
                              "index": 0
                            }
                         */
                        // TODO: Ask the machine for more data about this particular extruder
                        break;
                    default:
                        if (obj.ContainsKey("error"))
                        {
                            _model.Controller.Path.Alarm.Add(AdapterSdk.DataItems.Condition.Level.FAULT, obj["error"]["message"].ToString(), obj["error"]["code"].ToString(), obj["error"]["data"]["name"].ToString());
                        } else
                        {
                            _logger?.LogWarning("Unhandled message received: {@Message}", obj);
                        }
                        break;
                }
            } else
            {
                _logger?.LogWarning("Unrecognized, unsolicited message received: {@Message}", obj);
            }
            OnDataReceived?.Invoke(this, new DataReceivedEventArgs(_model));
        }

        private void ProcessSystemNotification(SystemNotification system_notification)
        {
            var model = system_notification?.@params?.info;
            // Update Tool Info
            if (model.toolheads != null)
            {
                foreach (var extruder in model.toolheads?.extruder)
                {
                    ToolHead ext = null;
                    if (extruder.index == 0)
                    {
                        ext = _model.Auxiliaries.ModelExtruder;
                    }
                    else if (extruder.index == 1)
                    {
                        ext = _model.Auxiliaries.SupportExtruder;
                    }
                    ext.CurrentTemperature = extruder.current_temperature;
                    ext.TargetTemperature = extruder.target_temperature;

                    ext.ExtruderId = extruder.tool_id.ToString();
                    // ext.Extruder is handled in the timer based on the ExtruderId being set above

                    // TODO: Track tool_present to update the Asset Changed

                    // Handle "Out-of-Filament"
                    if (extruder.filament_presence == null)
                    {
                        ext.OutOfFilament.Unavailable();
                        ext.FilamentType.Unavailable();
                    } else if (extruder.filament_presence == true)
                    {
                        ext.OutOfFilament = EndOfBar.PRIMARY.NO.ToString();
                    } else
                    {
                        ext.OutOfFilament = EndOfBar.PRIMARY.YES.ToString();
                        ext.FilamentType.Unavailable();
                    }

                    switch (extruder.error)
                    {
                        case 54:
                            ext.ToolError.Add(AdapterSdk.DataItems.Condition.Level.FAULT, "extruder not present", extruder.error.ToString());
                            break;
                        case 80:
                            ext.ToolError.Add(AdapterSdk.DataItems.Condition.Level.WARNING, "filament not present", extruder.error.ToString());
                            break;
                        case 81:
                            ext.ToolError.Add(AdapterSdk.DataItems.Condition.Level.FAULT, model.current_process?.step ?? "handling_recoverable_filament_jam", extruder.error.ToString());
                            break;
                        case null:
                        case 0:
                            ext.ToolError.Normal();
                            break;
                        default:
                            ext.ToolError.Add(AdapterSdk.DataItems.Condition.Level.FAULT, model.current_process?.step ?? extruder.error.ToString(), extruder.error.ToString());
                            _logger?.LogWarning("Unhandled toolheads.extruder[{ExtruderIndex}].error received: {ErrorCode}", extruder.index, extruder.error);
                            break;
                    }

                }
            } else
            {
                _model.Auxiliaries.Unavailable();
            }

            // Update process information

            _model.IPv4 = model.ip;
            _model.Port = Machine.Config.Port;
            _model.Controller.ApiVersion = model.api_version;
            _model.Controller.FirmwareVersion = model.firmware_version.ToString();

            if (model.current_process != null)
            {
                _model.Controller.Path.Program = model.current_process?.filepath
                    ?? model.current_process?.filename
                    ?? AdapterSdk.Constants.UNAVAILABLE;
                _model.Controller.Path.Username = model.current_process?.username
                    ?? AdapterSdk.Constants.UNAVAILABLE;

                // Keep track of reasons in the logging
                if (model.current_process?.reason != null)
                {
                    _logger?.LogWarning("Unhandled current_process.reason received: {@Reason}", model.current_process?.reason);
                }

                // Process EXECUTION
                if (model.current_process?.cancelled == true)
                {
                    _model.Controller.Path.Execution = Execution.INTERRUPTED;
                    _model.Controller.Path.State = ProcessState.ABORTED;
                    _model.Controller.Path.Functionality = FunctionalMode.TEARDOWN;
                }
                else if (model.current_process?.complete == true)
                {
                    _model.Controller.Path.Execution = Execution.PROGRAM_COMPLETED;

                    if (_model.Controller.Path.State?.ToString() != ProcessState.COMPLETE?.ToString())
                    {
                        _model.Controller.Path.State = ProcessState.COMPLETE;
                        _model.Controller.Path.PrintCompleted = DateTime.UtcNow.ToString();
                        _model.Controller.Path.Functionality = FunctionalMode.TEARDOWN;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.current_process?.step) && Enum.TryParse<PrinterStep>(model.current_process.step, out PrinterStep step))
                    {
                        _model.Controller.Path.Execution = GetExecutionValue(step);
                        _model.Controller.Path.State = GetProcessStateValue(step);
                        _model.Controller.Path.Functionality = GetFunctionalModeValue(step);
                    } else
                    {
                        _model.Controller.Path.Execution = Execution.READY;
                        _model.Controller.Path.State = ProcessState.READY;
                        _model.Controller.Path.Functionality?.Unavailable();
                    }
                }

                // Process SystemAlarm
                var error = model.current_process?.error;
                switch (error?.code)
                {
                    case null:
                        _model.Controller.Path.Alarm?.Normal();
                        break;
                    default:
                        _model.Controller.Path.Alarm.Add(AdapterSdk.DataItems.Condition.Level.FAULT, error.message, error.code);
                        _logger?.LogWarning("Unhandled current_process.error received: {@Error}", error);
                        break;
                }
            }
            else
            {
                _model.Controller.Path.Program?.Unavailable();
                _model.Controller.Path.PrintStart?.Unavailable();
                _model.Controller.Path.EstimatedCompletion?.Unavailable();
                _model.Controller.Path.PrintCompleted?.Unavailable();
                _model.Controller.Path.Execution = Execution.READY;
                _model.Controller.Path.Functionality?.Unavailable();
                _model.Controller.Path.State?.Unavailable();
                _model.Controller.Path.Alarm?.Normal();
            }
        }

        public ExecutionValues GetExecutionValue(PrinterStep? printerStep)
        {
            switch (printerStep)
            {
                case PrinterStep.running:
                case PrinterStep.printing:
                    return ExecutionValues.ACTIVE;
                case PrinterStep.initializing:
                case PrinterStep.initial_heating:
                case PrinterStep.final_heating:
                case PrinterStep.cooling:
                case PrinterStep.homing:
                case PrinterStep.position_found:
                case PrinterStep.preheating:
                case PrinterStep.calibrating:
                case PrinterStep.preheating_loading:
                case PrinterStep.preheating_resuming:
                case PrinterStep.preheating_unloading:
                case PrinterStep.stopping_filament:
                case PrinterStep.cleaning_up:
                case PrinterStep.loading_print_tool:
                case PrinterStep.waiting_for_file:
                case PrinterStep.extrusion:
                case PrinterStep.loading_filament:
                case PrinterStep.unloading_filament:
                case PrinterStep.transfer:
                case PrinterStep.downloading:
                case PrinterStep.verify_firmware:
                case PrinterStep.writing:
                case PrinterStep.suspending:
                case PrinterStep.unsuspending:
                case PrinterStep.clear_build_plate:
                case PrinterStep.clear_filament:
                case PrinterStep.remove_filament:
                    return ExecutionValues.WAIT;
                case PrinterStep.end_sequence:
                case PrinterStep.cancelling:
                    return ExecutionValues.PROGRAM_STOPPED;
                case PrinterStep.suspended:
                    return ExecutionValues.FEED_HOLD;
                case PrinterStep.failed:
                case PrinterStep.error_step:
                    return ExecutionValues.INTERRUPTED;
                case PrinterStep.completed:
                    return ExecutionValues.PROGRAM_COMPLETED;
                default:
                    return ExecutionValues.READY;
            }
        }
        public ProcessStateValues? GetProcessStateValue(PrinterStep? printerStep)
        {
            switch (printerStep)
            {
                case PrinterStep.running:
                case PrinterStep.printing:
                    return ProcessStateValues.ACTIVE;
                case PrinterStep.initializing:
                case PrinterStep.initial_heating:
                case PrinterStep.final_heating:
                case PrinterStep.homing:
                case PrinterStep.position_found:
                case PrinterStep.preheating:
                case PrinterStep.waiting_for_file:
                case PrinterStep.transfer:
                case PrinterStep.loading_filament:
                    return ProcessStateValues.INITIALIZING;
                case PrinterStep.cooling:
                case PrinterStep.calibrating:
                case PrinterStep.preheating_loading:
                case PrinterStep.preheating_resuming:
                case PrinterStep.preheating_unloading:
                case PrinterStep.stopping_filament:
                case PrinterStep.cleaning_up:
                case PrinterStep.loading_print_tool:
                case PrinterStep.extrusion:
                case PrinterStep.unsuspending:
                case PrinterStep.clear_build_plate:
                    return ProcessStateValues.READY;
                case PrinterStep.clear_filament:
                case PrinterStep.suspending:
                case PrinterStep.unloading_filament:
                case PrinterStep.remove_filament:
                    return ProcessStateValues.INTERRUPTED;
                case PrinterStep.cancelling:
                    return ProcessStateValues.ABORTED;
                case PrinterStep.suspended:
                case PrinterStep.failed:
                case PrinterStep.error_step:
                    return ProcessStateValues.INTERRUPTED;
                case PrinterStep.end_sequence:
                case PrinterStep.completed:
                    return ProcessStateValues.COMPLETE;
                case PrinterStep.downloading:
                case PrinterStep.writing:
                case PrinterStep.verify_firmware:
                default:
                    return null;
            }
        }
        public FunctionalModeValues? GetFunctionalModeValue(PrinterStep? printerStep)
        {
            switch (printerStep)
            {
                case PrinterStep.running:
                case PrinterStep.printing:
                case PrinterStep.unsuspending:
                case PrinterStep.suspending:
                    return FunctionalModeValues.PRODUCTION;
                case PrinterStep.initializing:
                case PrinterStep.initial_heating:
                case PrinterStep.final_heating:
                case PrinterStep.homing:
                case PrinterStep.position_found:
                case PrinterStep.preheating:
                case PrinterStep.waiting_for_file:
                case PrinterStep.transfer:
                case PrinterStep.clear_build_plate:
                case PrinterStep.loading_filament:
                    return FunctionalModeValues.SETUP;
                case PrinterStep.calibrating:
                case PrinterStep.preheating_loading:
                case PrinterStep.preheating_resuming:
                case PrinterStep.preheating_unloading:
                case PrinterStep.stopping_filament:
                case PrinterStep.cleaning_up:
                case PrinterStep.loading_print_tool:
                case PrinterStep.extrusion:
                case PrinterStep.clear_filament:
                case PrinterStep.unloading_filament:
                case PrinterStep.remove_filament:
                    return FunctionalModeValues.MAINTENANCE;
                case PrinterStep.cooling:
                case PrinterStep.end_sequence:
                case PrinterStep.completed:
                    return FunctionalModeValues.TEARDOWN;
                case PrinterStep.failed:
                case PrinterStep.error_step:
                case PrinterStep.suspended:
                case PrinterStep.cancelling:
                case PrinterStep.downloading:
                case PrinterStep.writing:
                case PrinterStep.verify_firmware:
                default:
                    return null;
            }
        }

        public void Stop(Exception ex = null)
        {
            _timer.Stop();

            if (Machine != null)
            {
                Machine.Stop();
                Machine.Connection.OnResponse -= Connection_OnResponse;
            }

            OnAdapterSourceStopped?.Invoke(this, new AdapterSourceStoppedEventArgs(ex));
        }

        private string ToUuid(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Set the version (4 bits) and variant (2 bits) according to the UUID specification
                hashBytes[7] = (byte)((hashBytes[7] & 0x0F) | 0x30); // version 3 (MD5)
                hashBytes[8] = (byte)((hashBytes[8] & 0x3F) | 0x80); // variant 1

                // Convert the hash bytes to a Guid
                return new Guid(hashBytes).ToString();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            Machine?.Dispose();
        }
    }
}
