using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot.Rpc
{
    public static class CroissantApi
    {
        private static async Task<JObject> request(this RpcConnection connection, string method, object body = null, CancellationToken cancellationToken = default)
        {
            JObject response = await connection.SendCommand(new RpcRequest(method, body), cancellationToken);
            if (response == null)
            {
                throw new Exception("Invalid RPC response");
            }

            return response;
        }

        public static async Task<JObject> AddLocalAuth(this RpcConnection connection, string username, string localSecret)
            => await connection.request("add_local_auth", new { username = username, local_secret = localSecret });


        public static async Task<JObject> AddMakerbotAccount(this RpcConnection connection, string username, string makerbotToken)
            => await connection.request("add_makerbot_account", new { username = username, makerbot_token = makerbotToken });

        public static async Task<JObject> Authenticate(this RpcConnection connection, string accessToken)
            => await connection.request("authenticate", new { access_token = accessToken });

        public static async Task<JObject> Authorize(this RpcConnection connection, string username, string makerbotToken = null, string localSecret = null, bool chamberBlink = false)
            => await connection.request("authorize", new { username = username, makerbot_token = makerbotToken, local_secret = localSecret, chamber_blink = chamberBlink });

        public static async Task<JObject> ClearAuthorize(this RpcConnection connection, string username)
            => await connection.request("deauthorize", new { username = username });

        public static async Task<JObject> GetAuthorized(this RpcConnection connection)
            => await connection.request("get_authorized");

        public static async Task<JObject> Reauthorize(this RpcConnection connection, string username, string makerbotToken = null, string localSecret = null, string localCode = null)
            => await connection.request("reauthorize", new { username = username, makerbot_token = makerbotToken, local_secret = localSecret, local_code = localCode });

        public static async Task<JObject> WifiFreAuthorize(this RpcConnection connection, string username, string makerbotToken = null, string localSecret = null)
            => await connection.request("wifi_fre_authorize", new { username = username, makerbot_token = makerbotToken, local_secret = localSecret });

        public static async Task<JObject> GetStaticIpv4(this RpcConnection connection, string servicePath)
            => await connection.request("get_static_ipv4", new { service_path = servicePath });

        public static async Task<JObject> SetStaticIpv4(this RpcConnection connection, string servicePath, string ip = null, string netmask = null, string gateway = null, object dns = null, bool useStatic = false)
            => await connection.request("set_static_ipv4", new
            {
                service_path = servicePath,
                ip = ip,
                netmask = netmask,
                gateway = gateway,
                dns = dns,
                use_static = useStatic
            });

        public static async Task<JObject> WifiConnect(this RpcConnection connection, string path, string password = null, string name = null)
            => await connection.request("wifi_connect", new
            {
                path = path,
                password = password,
                name = name
            });

        public static async Task<JObject> WifiDisable(this RpcConnection connection)
            => await connection.request("wifi_disabled");

        public static async Task<JObject> WifiDisconnect(this RpcConnection connection, string path)
            => await connection.request("wifi_disconnect", new { path = path });

        public static async Task<JObject> WifiEnable(this RpcConnection connection)
            => await connection.request("wifi_enable");

        public static async Task<JObject> WifiForget(this RpcConnection connection, string path = null)
            => await connection.request("wifi_forget", new { path = path });

        public static async Task<JObject> WifiReset(this RpcConnection connection)
            => await connection.request("wifi_reset");

        public static async Task<JObject> WifiScan(this RpcConnection connection, bool forceRescan = false)
            => await connection.request("wifi_scan", new { force_rescan = forceRescan });

        public static async Task<JObject> Acknowledged(this RpcConnection connection, int errorId)
            => await connection.request("acknowledged", new { error_id = errorId });

        public static async Task<JObject> AssistedLevel(this RpcConnection connection)
            => await connection.request("assisted_level");

        public static async Task<JObject> BotMaintained(this RpcConnection connection)
            => await connection.request("bot_maintained");

        public static async Task<JObject> BronxUpload(this RpcConnection connection, string filepath, int toolhead)
            => await connection.request("bronx_upload", new
            {
                filepath = filepath,
                toolhead = toolhead
            });

        public static async Task<JObject> BrooklynUpload(this RpcConnection connection, string filepath, bool transferWait = false)
            => await connection.request("brooklyn_upload", new
            {
                filepath = filepath,
                transfer_wait = transferWait
            });

        public static async Task<JObject> CalibrateZOffset(this RpcConnection connection)
            => await connection.request("calibrate_z_offset");

        public static async Task<JObject> Cancel(this RpcConnection connection, bool force = false)
            => await connection.request("cancel", new { force = force });

        public static async Task<JObject> ClearAllZPause(this RpcConnection connection)
            => await connection.request("clear_all_z_pause");

        public static async Task<JObject> ClearQueue(this RpcConnection connection)
            => await connection.request("clear_queue");

        public static async Task<JObject> ClearSshId(this RpcConnection connection, string filepath = "")
            => await connection.request("clear_ssh_id", new
            {
                filepath = filepath
            });

        public static async Task<JObject> ClearZPauseMm(this RpcConnection connection)
            => await connection.request("clear_z_pause_mm");

        public static async Task<JObject> CloseQueue(this RpcConnection connection)
            => await connection.request("close_queue");

        public static async Task<JObject> Cool(this RpcConnection connection, bool ignoreToolErrors = false)
            => await connection.request("cool", new
            {
                ignore_tool_errors = ignoreToolErrors
            });
        public static async Task<JObject> CopySshId(this RpcConnection connection, string filepath = "")
            => await connection.request("copy_ssh_id", new
            {
                filepath = filepath
            });
            
        public static async Task<JObject> DisableCheckBuildPlate(this RpcConnection connection)
        	=> await connection.request("disable_check_build_plate", null);
            
        public static async Task<JObject> DisableLeds(this RpcConnection connection, bool disableKnob = false, bool disableChamber = false)
        	=> await connection.request("disable_leds", new
            {
                disable_knob = disableKnob,
                disable_chamber = disableChamber
            });
            
        public static async Task<JObject> DisableZPause(this RpcConnection connection)
        	=> await connection.request("disable_z_pause", null);
            
        public static async Task<JObject> DownloadAndInstallFirmware(this RpcConnection connection)
        	=> await connection.request("download_and_install_firmware", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> DrmPrint(this RpcConnection connection, int layoutId)
        	=> await connection.request("drm_print", new
            {
                layout_id = layoutId
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> DumpMachineConfig(this RpcConnection connection, string path)
        	=> await connection.request("dump_machine_config", new
            {
                path = path
            });
            
        public static async Task<JObject> EnableCheckBuildPlate(this RpcConnection connection)
        	=> await connection.request("enable_check_build_plate", null);
            
        public static async Task<JObject> EnableLeds(this RpcConnection connection, bool enableKnob = false, bool enableChamber = false)
        	=> await connection.request("enable_leds", new
            {
                enable_knob = enableKnob,
                enable_chamber = enableChamber
            });
            
        public static async Task<JObject> EnableZPause(this RpcConnection connection)
        	=> await connection.request("enable_z_pause", null);
            
        public static async Task<Process> ExecuteQueue(this RpcConnection connection)
        	=> (await connection.request("execute_queue", null)).ToObject<Process>();

        /// <summary>
        /// Prints from an external .makerbot file
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <param name="url">Address of a .makerbot file</param>
        /// <param name="ensureBuildPlateClear">Override Check Build Plate Clear prompt</param>
        /// <returns></returns>
        public static async Task<JObject> ExternalPrint(this RpcConnection connection, string url, bool ensureBuildPlateClear = true)
        	=> await connection.request("external_print", new
            {
                url = url,
                ensure_build_plate_clear = ensureBuildPlateClear
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> FirmwareCleanup(this RpcConnection connection)
        	=> await connection.request("firmware_cleanup", null);
            
        public static async Task<double> GetAvailableZOffsetAdjustment(this RpcConnection connection)
        	=> (await connection.request("get_available_z_offset_adjustment", null))["result"].ToObject<double>();

        public static async Task<MakerBot.Rpc.MachineConfig> GetMachineConfig(this RpcConnection connection)
        	=> (await connection.request("get_machine_config", null)).ToObject<MakerBot.Rpc.MachineConfig>();

        public static async Task<JObject> GetPersistentStatistics(this RpcConnection connection)
        	=> await connection.request("get_persistent_statistics", null);
            
        public static async Task<JObject> GetPrintHistory(this RpcConnection connection)
        	=> await connection.request("get_print_history", null);
            
        public static async Task<QueueStatus> GetQueueStatus(this RpcConnection connection)
        	=> (await connection.request("get_queue_status", null)).ToObject<QueueStatus>();

        public static async Task<bool> GetSoundState(this RpcConnection connection)
        	=> (await connection.request("get_sound_state", null))["result"].ToObject<bool>();

        public static async Task<JObject> GetStatistics(this RpcConnection connection)
        	=> await connection.request("get_statistics", null);
            
        public static async Task<ToolUsageStats> GetToolUsageStats(this RpcConnection connection)
        	=> (await connection.request("get_tool_usage_stats", null)).ToObject<ToolUsageStats>();

        public static async Task<double> GetZAdjustedOffset(this RpcConnection connection)
        	=> (await connection.request("get_z_adjusted_offset", null))["result"].ToObject<double>();

        /// <summary>
        /// Queries the machine for calibration routine availability
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <returns>Whether the machine has the capability to run a z calibration routine</returns>
        public static async Task<bool> HasZCalibrationRoutine(this RpcConnection connection)
        	=> (await connection.request("has_z_calibration_routine", null))["result"].ToObject<bool>();

        [Obsolete("Method not tested")]
        public static async Task<JObject> Home(this RpcConnection connection, object axes, bool preheat = true)
        	=> await connection.request("home", new
            {
                axes = axes,
                preheat = preheat
            });
            

        /// <summary>
        /// Queries the machine for endstop status
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <returns>Whether an endstop is triggered or not</returns>
        public static async Task<bool> IsEndstopTriggered(this RpcConnection connection)
        	=> (await connection.request("is_endstop_triggered", null))["result"].ToObject<bool>();

        [Obsolete("Method not tested")]
        public static async Task<JObject> LibraryPrint(this RpcConnection connection, int layoutId)
        	=> await connection.request("library_print", new
            {
                layout_id = layoutId
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> LoadFilament(this RpcConnection connection, int toolIndex, object temperatureSettings = null)
        	=> await connection.request("load_filament", new
            {
                tool_index = toolIndex,
                temperature_settings = temperatureSettings
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> LoadPrintTool(this RpcConnection connection, int index)
        	=> await connection.request("load_print_tool", new
            {
                index = index
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> MachineActionCommand(this RpcConnection connection, string machineFunc, object parameters, string name = "", bool ignoreToolErrors = false)
            => await connection.request("machine_action_command", new
            {
                machine_func = machineFunc,
                @params = parameters,
                name = name,
                ignore_tool_errors = ignoreToolErrors
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> MachineQueryCommand(this RpcConnection connection, string machineFunc, object parameters, bool ignoreToolErrors = false)
            => await connection.request("machine_query_command", new
            {
                machine_func = machineFunc,
                @params = parameters,
                ignore_tool_errors = ignoreToolErrors
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> MachineQueryProcess(this RpcConnection connection, string machineFunc, object parameters, string name, bool ignoreToolErrors = false)
            => await connection.request("machine_query_process", new
            {
                machine_func = machineFunc,
                @params = parameters,
                name = name,
                ignore_tool_errors = ignoreToolErrors
            });
            
        public static async Task<Process> ManualLevel(this RpcConnection connection)
        	=> (await connection.request("manual_level", null)).ToObject<Process>();

        public static async Task<JObject> OpenQueue(this RpcConnection connection, bool clear)
        	=> await connection.request("open_queue", new
            {
                clear = clear
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> Park(this RpcConnection connection)
        	=> await connection.request("park", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> Preheat(this RpcConnection connection, object temperatureSettings = null)
        	=> await connection.request("preheat", new
            {
                temperature_settings = temperatureSettings
            });
            
        public static async Task<JObject> Print(this RpcConnection connection, string filepath, bool ensureBuildPlateClear = true, bool transferWait = true)
            => await connection.request("print", new
            {
                filepath = filepath,
                ensure_build_plate_clear = ensureBuildPlateClear,
                transfer_wait = transferWait
            });
            
        public static async Task<MachineActionProcess> PrintAgain(this RpcConnection connection)
        	=> (await connection.request("print_again", null)).ToObject<MachineActionProcess>();

        [Obsolete("Method not tested")]
        public static async Task<JObject> ProcessMethod(this RpcConnection connection, string method, object parameters = null)
            => await connection.request("process_method", new
            {
                method = method,
                @params = parameters
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> ResetToFactory(this RpcConnection connection, bool clearCalibration = false)
        	=> await connection.request("reset_to_factory", new
            {
                clear_calibration = clearCalibration
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> RunDiagnostics(this RpcConnection connection, object tests)
        	=> await connection.request("run_diagnostics", new
            {
                tests = tests
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetAutoUnload(this RpcConnection connection, string unloadCase)
        	=> await connection.request("set_auto_unload", new
            {
                unload_case = unloadCase
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetToolheadErrorVisibility(this RpcConnection connection, string error, bool ignored)
        	=> await connection.request("set_toolhead_error_visibility", new
            {
                error = error,
                ignored = ignored
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetZAdjustedOffset(this RpcConnection connection, double offset)
        	=> await connection.request("set_z_adjusted_offset", new
            {
                offset = offset
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetZPauseMm(this RpcConnection connection, int zPauseMm)
        	=> await connection.request("set_z_pause_mm", new
            {
                z_pause_mm = zPauseMm
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetupPrinter(this RpcConnection connection, bool jumpToWifiSetup = false)
        	=> await connection.request("setup_printer", new
            {
                jump_to_wifi_setup = jumpToWifiSetup
            });
            

        /// <summary>
        /// Toggles the sound state of the machine
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <param name="state">Set the state of sound</param>
        /// <returns></returns>
        public static async Task<JObject> ToggleSound(this RpcConnection connection, bool state)
        	=> await connection.request("toggle_sound", new
            {
                state = state
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> UnloadFilament(this RpcConnection connection, int toolIndex, object temperatureSettings = null)
        	=> await connection.request("unload_filament", new
            {
                tool_index = toolIndex,
                temperature_settings = temperatureSettings
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> WifiSetup(this RpcConnection connection)
        	=> await connection.request("wifi_setup", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> WifiSignalStrength(this RpcConnection connection, string ssid, string iface)
        	=> await connection.request("wifi_signal_strength", new
            {
                ssid = ssid,
                iface = iface
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> YonkersUpload(this RpcConnection connection, string filepath, string uid, int index, bool? force = null, int? id = null)
        	=> await connection.request("yonkers_upload", new
            {
                filepath = filepath,
                uid = uid,
                index = index,
                force = force,
                id = id
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> ZipLogs(this RpcConnection connection, string zipPath)
        	=> await connection.request("zip_logs", new
            {
                zip_path = zipPath
            });
            
        public static async Task<JObject> BirdwingList(this RpcConnection connection, string path)
        	=> await connection.request("birdwing_list", new
            {
                path = path
            });
            

        /// <summary>
        /// Requests the machine capture an image and save a JPG to the specified onboard location.
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <param name="outputFile">Storage location on the machine. Recommended path '/home/camera/' as it is accessible from the machine's settings.</param>
        /// <returns>Returns true if successfully saved</returns>
        public static async Task<bool> CaptureImage(this RpcConnection connection, string outputFile)
        {
            var obj = await connection.request("capture_image", new
            {
                output_file = outputFile
            });
            if (obj.ContainsKey("result"))
            {
                return obj["result"].ToObject<bool>();
            }
            else if (obj.ContainsKey("error"))
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the display name of the machine
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <param name="machineName">New display name for the machine</param>
        /// <returns></returns>
        public static async Task<JObject> ChangeMachineName(this RpcConnection connection, string machineName)
        	=> await connection.request("change_machine_name", new
            {
                machine_name = machineName
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> DesyncAccount(this RpcConnection connection)
        	=> await connection.request("desync_account", null);
            
        [Obsolete("Format not supported")]
        public static async Task<JObject> EndCameraStream(this RpcConnection connection)
            => await connection.request("end_camera_stream", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> ExpireThingiverseCredentials(this RpcConnection connection)
        	=> await connection.request("expire_thingiverse_credentials", null);
            

        /// <summary>
        /// Easter Egg
        /// </summary>
        /// <param name="cnn">Machine Connection</param>
        /// <returns></returns>
        public static async Task<string> FirstContact(this RpcConnection connection)
        	=> (await connection.request("first_contact", null))["result"].ToObject<string>();

        public static async Task<JObject> GetCloudServicesInfo(this RpcConnection connection)
        	=> await connection.request("get_cloud_services_info", null);
            
        public static async Task<Config> GetConfig(this RpcConnection connection)
        	=> (await connection.request("get_config", null)).ToObject<Config>();

        public static async Task<JObject> GetSystemInformation(this RpcConnection connection)
        {
            var obj = await connection.request("get_system_information", null);
            JsonSerializer _writer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Include
            };
            return obj;
        }

        public static async Task<Handshake> Handshake(this RpcConnection connection, string username = null, string hostVersion = null)
        	=> (await connection.request("handshake", new
            {
                username = username,
                host_version = hostVersion
            })).ToObject<Handshake>();
            
        public static async Task<NetworkState> NetworkState(this RpcConnection connection)
        	=> (await connection.request("network_state", null)).ToObject<NetworkState>();

        public static async Task<JObject> Ping(this RpcConnection connection)
        	=> await connection.request("ping", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> RegisterClientName(this RpcConnection connection, string name)
        	=> await connection.request("register_client_name", new
            {
                name = name
            });
            
        [Obsolete("Format not supported")]
        public static async Task<JObject> RequestCameraFrame(this RpcConnection connection)
            => await connection.request("request_camera_frame", null);
            
        [Obsolete("Format not supported")]
        public static async Task<JObject> RequestCameraStream(this RpcConnection connection)
            => await connection.request("request_camera_stream", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetAnalyticsEnabled(this RpcConnection connection, bool enabled)
        	=> await connection.request("set_analytics_enabled", new
            {
                enabled = enabled
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetReflectorEnabled(this RpcConnection connection, bool enabled)
        	=> await connection.request("set_reflector_enabled", new
            {
                enabled = enabled
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SetThingiverseCredentials(this RpcConnection connection, string thingiverseUsername, string thingiverseToken)
        	=> await connection.request("set_thingiverse_credentials", new
            {
                thingiverse_username = thingiverseUsername,
                thingiverse_token = thingiverseToken
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> SyncAccountToBot(this RpcConnection connection)
        	=> await connection.request("sync_account_to_bot", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> UpdateAvailableFirmware(this RpcConnection connection)
        	=> await connection.request("update_available_firmware", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> GetInit(this RpcConnection connection, string filePath, string fileId)
        	=> await connection.request("get_init", new
            {
                file_path = filePath,
                file_id = fileId
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> PutInit(this RpcConnection connection, string filePath, string fileId)
        	=> await connection.request("put_init", new
            {
                file_path = filePath,
                file_id = fileId
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> PutTerm(this RpcConnection connection, string fileId, int length, uint crc)
        	=> await connection.request("put_term", new
            {
                file_id = fileId,
                length = length,
                crc = crc
            });
            
        [Obsolete("Method not found")]
        public static async Task<JObject> GetUniqueIdentifiers(this RpcConnection connection)
        	=> await connection.request("get_unique_identifiers", null);
            
        [Obsolete("Method not found")]
        public static async Task<JObject> GetSpoolInfo(this RpcConnection connection, int bayIndex)
        	=> await connection.request("get_spool_info", new
            {
                bay_index = bayIndex
            });
            
        [Obsolete("Method not found")]
        public static async Task<JObject> GetTrackedStats(this RpcConnection connection)
        	=> await connection.request("get_tracked_stats", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> PutRaw(this RpcConnection connection, string fileId, char block, object blockSize)
        	=> await connection.request("put_raw", new
            {
                file_id = fileId,
                block = block,
                block_size = blockSize
            });
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> ReflectorAuth(this RpcConnection connection, object authInfo)
        	=> await connection.request("reflector_auth", new
            {
                auth_info = authInfo
            });
            

        // JsonRpcNotification

        [Obsolete("Method not tested")]
        public static async Task<JObject> SystemNotification(this RpcConnection connection, object info)
            => throw new NotImplementedException();

        [Obsolete("Method not tested")]
        public static async Task<JObject> StateNotification(this RpcConnection connection, object info)
            => throw new NotImplementedException();
        
        [Obsolete("Method not tested")]
        public static async Task<JObject> ErrorNotification(this RpcConnection connection, int code, int errorId, object source)
            => throw new NotImplementedException();

        [Obsolete("Method not tested")]
        public static async Task<JObject> ErrorAcknowledged(this RpcConnection connection, int errorId)
            => throw new NotImplementedException();

        [Obsolete("Method not tested")]
        public static async Task<JObject> GetTerm(this RpcConnection connection, string id, uint crc)
            => throw new NotImplementedException();

        [Obsolete("Method not supported")]
        public static async Task<JObject> CameraFrame(this RpcConnection connection)
            => await connection.request("camera_frame", null);
            
        [Obsolete("Method not tested")]
        public static async Task<JObject> ExtruderChange(this RpcConnection connection, int index, object config)
            => throw new NotImplementedException();

        [Obsolete("Method not tested")]
        public static async Task<JObject> NetworkStateChange(this RpcConnection connection, object state)
            => throw new NotImplementedException();

        [Obsolete("Method not tested")]
        public static async Task<JObject> FirmwareUpdatesInfoChange(this RpcConnection connection, string version, bool updateAvailable, bool isOnline, int error, string releaseDate, string releaseNotes)
            =>throw new NotImplementedException();

        // JsonRpcMethod
        [Obsolete("Method not tested")]
        public static async Task<JObject> GetRaw(this RpcConnection connection, string id, int length)
            => await connection.request("get_raw", new
            {
                id = id,
                length = length
            });
            

        [Obsolete("Method not supported")]
        public static async Task<string> GetRawCameraImageData(this RpcConnection connection)
        {
            string obj = await FastCGI.Send(connection.Endpoint.Address, "camera", new
            {
                token = connection.AccessTokens[FastCGI.AccessTokenContexts.camera]
            });
            return obj;
        }
            
    }
}
