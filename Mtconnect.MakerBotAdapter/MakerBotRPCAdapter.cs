using MakerBot;
using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Mtconnect.AdapterInterface.DataItemValues;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Threading;
using static MakerBot.Rpc.SystemInformation.Result.Toolheads;
using static System.Net.Mime.MediaTypeNames;

namespace Mtconnect.MakerBotAdapter
{
    public class MakerBotRPCAdapter : IAdapterSource, IDisposable
    {
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
            } else if (!Machine.Connection.IsAuthenticated)
            {
            }

            _model.Controller.Path.ToolOffset = Convert.ToInt32(Machine.Connection.GetZAdjustedOffset().Result);

            _busy = false;

            OnDataReceived?.Invoke(_model, new DataReceivedEventArgs());
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
                            _model.Controller.Path.Alarm.Add(AdapterInterface.DataItems.Condition.Level.FAULT, obj["error"]["message"].ToString(), obj["error"]["code"].ToString(), obj["error"]["data"]["name"].ToString());
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
            OnDataReceived?.Invoke(_model, new DataReceivedEventArgs());
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

                    ext.ToolId = extruder.tool_id.ToString();
                    // TODO: Use the Tool_Id to potentially predict what type of material is loaded.
                    switch (extruder.tool_id)
                    {
                        case 0:
                            // No extruder present
                            ext.ToolId = "UNAVAILABLE";
                            break;
                        case 8:
                            // PLA EXTRUDER
                            break;
                        case 14:
                            // TOUGH EXTRUDER
                            break;
                        case 99:
                            // EXPERIMENTAL EXTRUDER
                            break;
                        default:
                            _logger?.LogWarning("Unhandled toolheads.extruder[{ExtruderIndex}].tool_id received: {ToolId}", extruder.index, extruder.tool_id);
                            break;
                    }
                    // TODO: Track tool_present to update the Asset Changed

                    // Handle "Out-of-Filament"
                    if (extruder.filament_presence == null)
                    {
                        ext.OutOfFilament = "UNAVAILABLE";
                    } else if (extruder.filament_presence == true)
                    {
                        ext.OutOfFilament = EndOfBar.PRIMARY.NO.ToString();
                    } else
                    {
                        ext.OutOfFilament = EndOfBar.PRIMARY.YES.ToString();
                    }

                    switch (extruder.error)
                    {
                        case 54:
                            ext.ToolError.Add(AdapterInterface.DataItems.Condition.Level.FAULT, "extruder not present", extruder.error.ToString());
                            break;
                        case 80:
                            ext.ToolError.Add(AdapterInterface.DataItems.Condition.Level.WARNING, "filament not present", extruder.error.ToString());
                            break;
                        case 81:
                            ext.ToolError.Add(AdapterInterface.DataItems.Condition.Level.FAULT, model.current_process?.step ?? "handling_recoverable_filament_jam", extruder.error.ToString());
                            break;
                        case null:
                        case 0:
                            ext.ToolError.Normal();
                            break;
                        default:
                            ext.ToolError.Add(AdapterInterface.DataItems.Condition.Level.FAULT, model.current_process?.step ?? extruder.error.ToString(), extruder.error.ToString());
                            _logger?.LogWarning("Unhandled toolheads.extruder[{ExtruderIndex}].error received: {ErrorCode}", extruder.index, extruder.error);
                            break;
                    }

                }
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
                    ?? "UNAVAILABLE";
                _model.Controller.Path.Username = model.current_process?.username
                    ?? "UNAVAILABLE";

                // Keep track of reasons in the logging
                if (model.current_process?.reason != null)
                {
                    _logger?.LogWarning("Unhandled current_process.reason received: {@Reason}", model.current_process?.reason);
                }

                // Keep track of process ids in the logging
                switch (model.current_process?.id)
                {
                    case null:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Idle");
                        break;
                    case 1:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Ready");
                        break;
                    case 4:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Print Job");
                        break;
                    case 5:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "LoadFilamentProcess");
                        break;
                    case 6:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Loading Filament (PLA)");
                        break;
                    case 8:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Loading Filament (Experimental Extruder)");
                        break;
                    case 10:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Loading Filament (Experimental Extruder)");
                        break;
                    case 11:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Machine Action: Go Back");
                        break;
                    case 12:
                        _logger?.LogInformation("Recognizing Process Id ({ProcessId}) as {ProcessName}", model.current_process?.id, "Clear build plate");
                        break;
                    default:
                        _logger?.LogWarning("Unhandled current_process.id received: {ProcessId}\r\nStep: {ProcessStep}", model.current_process?.id, model.current_process.step);
                        break;
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

                    switch (model.current_process.name)
                    {
                        case "PrintProcess":
                            _model.Controller.Path.Functionality = FunctionalMode.PRODUCTION;
                            switch (model.current_process.step)
                            {
                                case "initializing":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    if (_model.Controller.Path.State != ProcessState.INITIALIZING)
                                    {
                                        _model.Controller.Path.State = ProcessState.INITIALIZING;
                                        _model.Controller.Path.PrintStart = DateTime.UtcNow.ToString();
                                    }
                                    break;
                                case "initial_heating":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    _model.Controller.Path.State = ProcessState.INITIALIZING;
                                    break;
                                case "final_heating":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    _model.Controller.Path.State = ProcessState.INITIALIZING;
                                    break;
                                case "running": // FROM PID 6,8
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    break;
                                case "homing":
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    _model.Controller.Path.State = ProcessState.INITIALIZING;
                                    break;
                                case "preheating":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    _model.Controller.Path.State = ProcessState.INITIALIZING;
                                    break;
                                case "extrusion": // FROM PID 6,8
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    break;
                                case "printing":
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    _model.Controller.Path.State = ProcessState.ACTIVE;
                                    break;
                                case "unsuspending":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    _model.Controller.Path.State = ProcessState.ACTIVE;
                                    break;
                                case "handling_recoverable_filament_jam":
                                    _model.Controller.Path.Execution = Execution.WAIT;
                                    _model.Controller.Path.State = ProcessState.INTERRUPTED;
                                    _model.Controller.Path.Functionality = FunctionalMode.MAINTENANCE;
                                    break;
                                case "suspended":
                                    _model.Controller.Path.Execution = Execution.INTERRUPTED;
                                    _model.Controller.Path.State = ProcessState.INTERRUPTED;
                                    _model.Controller.Path.Functionality = FunctionalMode.MAINTENANCE;
                                    break;
                                default:
                                    _logger?.LogWarning("Unhandled current_process.step received in {ProcessName}: {ProcessStep}", model.current_process.name, model.current_process.step);
                                    break;
                            }
                            break;
                        case "LoadFilamentProcess":
                            _model.Controller.Path.Execution = Execution.WAIT;
                            _model.Controller.Path.State = ProcessState.INTERRUPTED;
                            _model.Controller.Path.Functionality = FunctionalMode.SETUP;
                            break;
                        case "MachineActionProcess":
                            switch (model.current_process.step)
                            {
                                case "running":
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    _model.Controller.Path.Functionality = FunctionalMode.MAINTENANCE;
                                    break;
                                case "cleaning_up":
                                    _model.Controller.Path.Execution = Execution.ACTIVE;
                                    break;
                                case "done":
                                    _model.Controller.Path.Execution = Execution.READY;
                                    break;
                                default:
                                    _logger?.LogWarning("Unhandled current_process.step received in {ProcessName}: {ProcessStep}", model.current_process.name, model.current_process.step);
                                    break;
                            }
                            break;
                        default:
                            _logger?.LogWarning("Unhandled current_process.name received: {ProcessName}", model.current_process.name);
                            break;
                    }
                }

                // Process SystemAlarm
                var error = model.current_process?.error;
                switch (error?.code)
                {
                    case null:
                        _model.Controller.Path.Alarm.Normal();
                        break;
                    default:
                        _model.Controller.Path.Alarm.Add(AdapterInterface.DataItems.Condition.Level.FAULT, error.message, error.code);
                        _logger?.LogWarning("Unhandled current_process.error received: {@Error}", error);
                        break;
                }
            }
            else
            {
                _model.Controller.Path.Program = "UNAVAILABLE";
                _model.Controller.Path.PrintStart = "UNAVAILABLE";
                _model.Controller.Path.EstimatedCompletion = "UNAVAILABLE";
                _model.Controller.Path.PrintCompleted = "UNAVAILABLE";
                _model.Controller.Path.Execution = Execution.READY;
                _model.Controller.Path.Functionality = "UNAVAILABLE";
                _model.Controller.Path.State = "UNAVAILABLE";
                _model.Controller.Path.Alarm.Normal();
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

        public void Dispose()
        {
            _timer?.Dispose();
            Machine?.Dispose();
        }
    }
}
