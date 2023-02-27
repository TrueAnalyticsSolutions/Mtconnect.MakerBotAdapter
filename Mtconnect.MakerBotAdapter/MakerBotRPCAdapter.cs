using MakerBot;
using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace Mtconnect.MakerBotAdapter
{
    public class MakerBotRPCAdapter : IAdapterSource, IDisposable
    {
        private ILogger _logger { get; set; }
        
        public event DataReceivedHandler OnDataReceived;
        public event AdapterSourceStartedHandler OnAdapterSourceStarted;
        public event AdapterSourceStoppedHandler OnAdapterSourceStopped;

        private bool _busy { get; set; }

        private System.Timers.Timer _timer { get; set; } = new System.Timers.Timer();

        public MakerBot.Machine Machine { get; set; }

        private MakerBotMachine _model { get; set; } = new MakerBotMachine();

        public MakerBotRPCAdapter(string address, int port = 9999, string authCode = null, double pollRate = 5_000, ILoggerFactory loggerFactory = default)
        {
            if (pollRate <= 0) throw new IndexOutOfRangeException("Poll rate cannot be less than or equal to zero");

            _logger = loggerFactory?.CreateLogger<MakerBotRPCAdapter>();

            IPAddress ipAddress = null;
            if (!IPAddress.TryParse(address, out ipAddress))
            {
                throw new ArgumentException("Failed to parse machine address", nameof(address));
            }

            Machine = new MakerBot.Machine(ipAddress, port, loggerFactory);
            Machine.Config.Address = ipAddress.ToString();
            Machine.Config.Port = port;

            if (!string.IsNullOrEmpty(authCode))
            {
                _logger?.LogInformation("Using Authorization Code: {AuthCode}", authCode);
                Machine.Config.AuthenticationCode = authCode;
            } else
            {
                _logger?.LogWarning("Recommended to provide an authCode for the RPC connection");
            }

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
                _model.Availability = "UNAVAILABLE";
            } else if (!Machine.Connection.IsAuthenticated)
            {
            }

            _model.ToolOffset = Machine.Connection.GetZAdjustedOffset().Result;

            _busy = false;

            OnDataReceived?.Invoke(_model, new DataReceivedEventArgs());
        }

        public void Start(CancellationToken token = default)
        {
            Machine.Connection.OnResponse += Connection_OnResponse;

            using (var machineFactory = new MachineFactory())
            {
                var discoveries = machineFactory.Discover();
                if (discoveries != null)
                {
                    var match = discoveries.FirstOrDefault(o => o.ip == Machine.Config.Address && o.port == Machine.Config.Port.ToString());
                    if (match != null)
                    {
                        _model.Port = int.Parse(match.port);
                        _model.IPv4 = match.ip;
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
            Machine.Start("MTConnectAdapter", token);

            // TODO: Get more machine information to fill in config
            var sysInfo = Machine.Connection.GetSystemInformation().Result;
            if (sysInfo != null)
            {
                _logger?.LogDebug("Received SystemInformation: ${SysInfo}", JsonConvert.SerializeObject(sysInfo));
            } else
            {
                _logger?.LogDebug("Could not retrieve SystemInformation");
            }


            _timer.Start();

            OnAdapterSourceStarted?.Invoke(this, new AdapterSourceStartedEventArgs());
        }

        private void Connection_OnResponse(Newtonsoft.Json.Linq.JObject obj)
        {
            _model.Availability = "AVAILABLE";

            if (obj.ContainsKey("method"))
            {
                string method = obj["method"].ToString();
                switch (method)
                {
                    case "system_notification":
                        var system_notification = obj.ToObject<SystemNotification>();
                        if (system_notification?.@params == null)
                        {
                            _logger?.LogWarning("Could not parse 'system_notification': ${Result}", JsonConvert.SerializeObject(obj));
                        }

                        var model = system_notification?.@params?.info;
                        // Update Tool Info
                        if (model.toolheads != null)
                        {
                            foreach (var extruder in model.toolheads?.extruder)
                            {
                                ToolHead ext = null;
                                if (extruder.index == 0)
                                {
                                    ext = _model.Extruder1;
                                }
                                else if (extruder.index == 1)
                                {
                                    ext = _model.Extruder2;
                                }
                                ext.CurrentTemperature = extruder.current_temperature;
                                ext.TargetTemperature = extruder.target_temperature;
                                ext.ToolId = extruder.tool_id.ToString();
                                if (extruder.error == 0)
                                {
                                    ext.ToolError.Normal();
                                }
                                else
                                {
                                    ext.ToolError.Add(AdapterInterface.DataItems.Condition.Level.FAULT, extruder.error.ToString(), extruder.error.ToString());
                                }
                            }
                        }

                        // Update process information
                        // TODO: Tie-in to current_process

                        _model.IPv4 = model.ip;
                        _model.Port = Machine.Config.Port;

                        if (model.current_process != null)
                        {
                            _model.Program = model.current_process?.name
                                ?? model.current_process?.filepath
                                ?? model.current_process?.filename
                                ?? "UNAVAILABLE";

                            // Process EXECUTION
                            if (model.current_process?.cancelled == true)
                            {
                                _model.Execution = "INTERRUPTED";
                            }
                            else if (model.current_process?.complete == true)
                            {
                                _model.Execution = "PROGRAM_COMPLETED";
                            }
                            else
                            {
                                _model.Execution = "ACTIVE";
                            }

                            // Process SystemAlarm
                            var error = model.current_process?.error;
                            if (error != null && !string.IsNullOrEmpty(error.code))
                            {
                                _model.Alarm.Add(AdapterInterface.DataItems.Condition.Level.FAULT, error.message, error.code);
                            }
                        }
                        else
                        {
                            _model.Program = "UNAVAILABLE";
                            _model.Execution = "READY";
                            _model.Alarm.Normal();
                        }
                        break;
                    default:
                        if (obj.ContainsKey("error"))
                        {
                            _model.Alarm.Add(AdapterInterface.DataItems.Condition.Level.FAULT, obj["error"]["message"].ToString(), obj["error"]["code"].ToString(), obj["error"]["data"]["name"].ToString());
                        } else
                        {
                            _logger?.LogWarning("Unhandled message received: ${Message}", JsonConvert.SerializeObject(obj));
                        }
                        break;
                }
            } else
            {
                _logger?.LogWarning("Unrecognized, unsolicited message received: ${Message}", JsonConvert.SerializeObject(obj));
            }
            OnDataReceived?.Invoke(_model, new DataReceivedEventArgs());
        }

        public void Stop(Exception ex = null)
        {
            _timer.Stop();
            Machine.Stop();
            Machine.Connection.OnResponse -= Connection_OnResponse;

            OnAdapterSourceStopped?.Invoke(this, new AdapterSourceStoppedEventArgs(ex));
        }

        public void Dispose()
        {
            _timer?.Dispose();
            Machine?.Dispose();
        }
    }
}
