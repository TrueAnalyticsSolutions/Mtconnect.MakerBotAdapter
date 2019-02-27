using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public static class CroissantAPI{

    // Asynchronous Methods
    public static JObject AddLocalAuth(this Makerbot.CommunicationConnection cnn, string username, string localSecret){
      JObject obj = cnn.RawRequest("add_local_auth", new {
        username = username,
        local_secret = localSecret
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject AddMakerbotAccount(this Makerbot.CommunicationConnection cnn, string username, string makerbotToken) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("add_makerbot_account", new {
        username = username,
        makerbot_token = makerbotToken
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject Authenticate(this Makerbot.CommunicationConnection cnn, string accessToken) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("authenticate", new {
        access_token = accessToken
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject Authorize(this Makerbot.CommunicationConnection cnn, string username, string makerbotToken = null, string localSecret = null, bool chamberBlink = false) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("authorize", new {
        username = username,
        makerbot_token = makerbotToken,
        local_secret = localSecret,
        chamber_blink = chamberBlink
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ClearAuthorize(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("clear_authorize", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject Deauthorize(this Makerbot.CommunicationConnection cnn, string username) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("deauthorize", null);
      return obj;
    }
    
    /// <summary>
    /// Gets connected authorized accounts
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <returns></returns>
    public static DTOs.RPC.Authorizations GetAuthorized(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_authorized", null);
      return obj.ToObject<DTOs.RPC.Authorizations>();
    }
    [Obsolete("Method not tested")]
    public static JObject Reauthorize(this Makerbot.CommunicationConnection cnn, string username, string makerbotToken=null,string localSecret = null, string localCode=null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("reauthorize", new {
        username = username,
        makerbot_token = makerbotToken,
        local_secret = localSecret,
        local_code = localCode
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiFreAuthorize(this Makerbot.CommunicationConnection cnn, string username, string makerbotToken = null, string localSecret = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_fre_authorize", new {
        username = username,
        makerbot_token = makerbotToken,
        local_secret = localSecret
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject GetStaticIpv4(this Makerbot.CommunicationConnection cnn, string servicePath) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("get_static_ipv4", new {
        service_path = servicePath
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetStaticIpv4(this Makerbot.CommunicationConnection cnn, string servicePath, string ip=null, string netmask=null, string gateway = null, object dns = null, bool useStatic = false) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_static_ipv4", new {
        service_path = servicePath,
        ip = ip,
        netmask = netmask,
        gateway = gateway,
        dns = dns, use_static = useStatic
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiConnect(this Makerbot.CommunicationConnection cnn, string path, string password = null, string name = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_connect", new {
        path = path,
        password = password,
        name = name
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiDisable(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_disabled", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiDisconnect(this Makerbot.CommunicationConnection cnn, string path = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_disconnect", new {
        path = path
      });
      return obj;
    }
    
    /// <summary>
    /// Enables onboard wireless communication hardware
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <returns></returns>
    public static JObject WifiEnable(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("wifi_enable", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiForget(this Makerbot.CommunicationConnection cnn, string path = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_forget", new {
        path = path
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiReset(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_reset", null);
      return obj;
    }
    
    /// <summary>
    /// Scans for WiFi signals using the machine's onboard wireless hardware
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <param name="forceRescan">Toggle whether to force a rescan if already currently scanning.</param>
    /// <returns>List of nearby wireless access points</returns>
    public static DTOs.RPC.WiFiAccessPoints WifiScan(this Makerbot.CommunicationConnection cnn, bool forceRescan = false) {
      JObject obj = cnn.RawRequest("wifi_scan", new {
        force_rescan = forceRescan
      });
      return obj.ToObject<DTOs.RPC.WiFiAccessPoints>();
    }
    [Obsolete("Method not tested")]
    public static JObject Acknowledged(this Makerbot.CommunicationConnection cnn, int errorId) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("acknowledged", new {
        error_id = errorId
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject AssistedLevel(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("assisted_level", null);
      return obj;
    }
    public static JObject BotMaintained(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("bot_maintained", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject BronxUpload(this Makerbot.CommunicationConnection cnn, string filepath, int toolhead) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("bronx_upload", new {
        filepath = filepath,
        toolhead = toolhead
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject BrooklynUpload(this Makerbot.CommunicationConnection cnn, string filepath, bool transferWait = false) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("brooklyn_upload", new {
        filepath = filepath,
        transfer_wait = transferWait
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject CalibrateZOffset(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("calibrate_z_offset", null);
      return obj;
    }
    public static JObject Cancel(this Makerbot.CommunicationConnection cnn, bool force = false) {
      JObject obj = cnn.RawRequest("cancel", new {
        force = force
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ClearAllZPause(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("clear_all_z_pause", null);
      return obj;
    }
    public static JObject ClearQueue(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("clear_queue", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ClearSshId(this Makerbot.CommunicationConnection cnn, string filepath = "") {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("clear_ssh_id", new {
        filepath = filepath
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ClearZPauseMm(this Makerbot.CommunicationConnection cnn, int zPauseMm) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("clear_z_pause_mm", null);
      return obj;
    }
    public static JObject CloseQueue(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("close_queue", null);
      return obj;
    }
    public static DTOs.RPC.MachineActionProcess Cool(this Makerbot.CommunicationConnection cnn, bool ignoreToolErrors = false) {
      JObject obj = cnn.RawRequest("cool", new {
        ignore_tool_errors = ignoreToolErrors
      });
      return obj.ToObject<DTOs.RPC.MachineActionProcess>();
    }
    public static JObject CopySshId(this Makerbot.CommunicationConnection cnn, string filepath = "") {
      JObject obj = cnn.RawRequest("copy_ssh_id", new {
        filepath = filepath
      });
      return obj;
    }
    public static JObject DisableCheckBuildPlate(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("disable_check_build_plate", null);
      return obj;
    }
    public static JObject DisableLeds(this Makerbot.CommunicationConnection cnn, bool disableKnob = false, bool disableChamber = false) {
      JObject obj = cnn.RawRequest("disable_leds", new {
        disable_knob = disableKnob,
        disable_chamber = disableChamber
      });
      return obj;
    }
    public static JObject DisableZPause(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("disable_z_pause", null);
      return obj;
    }
    public static JObject DownloadAndInstallFirmware(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("download_and_install_firmware", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject DrmPrint(this Makerbot.CommunicationConnection cnn, int layoutId) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("drm_print", new {
        layout_id = layoutId
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject DumpMachineConfig(this Makerbot.CommunicationConnection cnn, string path) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("dump_machine_config", new {
        path = path
      });
      return obj;
    }
    public static JObject EnableCheckBuildPlate(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("enable_check_build_plate", null);
      return obj;
    }
    public static JObject EnableLeds(this Makerbot.CommunicationConnection cnn, bool enableKnob = false, bool enableChamber=false) {
      JObject obj = cnn.RawRequest("enable_leds", new {
        enable_knob = enableKnob,
        enable_chamber = enableChamber
      });
      return obj;
    }
    public static JObject EnableZPause(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("enable_z_pause", null);
      return obj;
    }
    public static DTOs.RPC.Process ExecuteQueue(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("execute_queue", null);
      return obj.ToObject<DTOs.RPC.Process>();
    }
    
    /// <summary>
    /// Prints from an external .makerbot file
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <param name="url">Address of a .makerbot file</param>
    /// <param name="ensureBuildPlateClear">Override Check Build Plate Clear prompt</param>
    /// <returns></returns>
    public static JObject ExternalPrint(this Makerbot.CommunicationConnection cnn, string url, bool ensureBuildPlateClear = true) {
      JObject obj = cnn.RawRequest("external_print", new {
        url = url,
        ensure_build_plate_clear = ensureBuildPlateClear
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject FirmwareCleanup(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("firmware_cleanup", null);
      return obj;
    }
    public static double GetAvailableZOffsetAdjustment(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_available_z_offset_adjustment", null);
      return obj["result"].ToObject<double>();
    }
    public static DTOs.RPC.MachineConfig GetMachineConfig(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_machine_config", null);
      return obj.ToObject<DTOs.RPC.MachineConfig>();
    }
    public static JObject GetPersistentStatistics(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_persistent_statistics", null);
      return obj;
    }
    public static JObject GetPrintHistory(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_print_history", null);
      return obj;
    }
    public static DTOs.RPC.QueueStatus GetQueueStatus(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_queue_status", null);
      return obj.ToObject<DTOs.RPC.QueueStatus>();
    }
    public static bool GetSoundState(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_sound_state", null);
      return obj["result"].ToObject<bool>();
    }
    public static JObject GetStatistics(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_statistics", null);
      return obj;
    }
    public static DTOs.RPC.ToolUsageStats GetToolUsageStats(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_tool_usage_stats", null);
      return obj.ToObject<DTOs.RPC.ToolUsageStats>();
    }
    public static double GetZAdjustedOffset(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_z_adjusted_offset", null);
      return obj["result"].ToObject<double>();
    }
    
    /// <summary>
    /// Queries the machine for calibration routine availability
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <returns>Whether the machine has the capability to run a z calibration routine</returns>
    public static bool HasZCalibrationRoutine(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("has_z_calibration_routine", null);
      return obj["result"].ToObject<bool>();
    }
    [Obsolete("Method not tested")]
    public static JObject Home(this Makerbot.CommunicationConnection cnn, object axes, bool preheat = true) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("home", new {
        axes = axes,
        preheat = preheat
      });
      return obj;
    }
    
    /// <summary>
    /// Queries the machine for endstop status
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <returns>Whether an endstop is triggered or not</returns>
    public static bool IsEndstopTriggered(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("is_endstop_triggered", null);
      return obj["result"].ToObject<bool>();
    }
    [Obsolete("Method not tested")]
    public static JObject LibraryPrint(this Makerbot.CommunicationConnection cnn, int layoutId) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("library_print", new {
        layout_id = layoutId
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject LoadFilament(this Makerbot.CommunicationConnection cnn, int toolIndex, object temperatureSettings = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("load_filament", new {
        tool_index = toolIndex,
        temperature_settings = temperatureSettings
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject LoadPrintTool(this Makerbot.CommunicationConnection cnn, int index) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("load_print_tool", new {
        index = index
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject MachineActionCommand(this Makerbot.CommunicationConnection cnn, string machineFunc, object parameters, string name = "", bool ignoreToolErrors = false) {
      // parameters = params
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("machine_action_command", new {
        machine_func = machineFunc,
        @params = parameters,
        name = name, 
        ignore_tool_errors = ignoreToolErrors
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject MachineQueryCommand(this Makerbot.CommunicationConnection cnn, string machineFunc, object parameters, bool ignoreToolErrors = false) {
      // parameters = params
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("machine_query_command", new {
        machine_func = machineFunc,
        @params = parameters,
        ignore_tool_errors = ignoreToolErrors
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject MachineQueryProcess(this Makerbot.CommunicationConnection cnn, string machineFunc, object parameters, string name, bool ignoreToolErrors = false) {
      // parameters = params
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("machine_query_process", new {
        machine_func = machineFunc,
        @params = parameters,
        name = name,
        ignore_tool_errors = ignoreToolErrors
      });
      return obj;
    }
    public static DTOs.RPC.Process ManualLevel(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("manual_level", null);
      return obj.ToObject<DTOs.RPC.Process>();
    }
    public static JObject OpenQueue(this Makerbot.CommunicationConnection cnn, bool clear) {
      JObject obj = cnn.RawRequest("open_queue", new {
        clear = clear
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject Park(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("park", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject Preheat(this Makerbot.CommunicationConnection cnn, object temperatureSettings = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("preheat", new {
        temperature_settings = temperatureSettings
      });
      return obj;
    }
    public static JObject Print(this Makerbot.CommunicationConnection cnn, string filepath, bool ensureBuildPlateClear = true, bool transferWait = true) {
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("print", new {
        filepath = filepath,
        ensure_build_plate_clear = ensureBuildPlateClear,
        transfer_wait = transferWait
      });
      return obj;
    }
    public static DTOs.RPC.MachineActionProcess PrintAgain(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("print_again", null);
      return obj.ToObject<DTOs.RPC.MachineActionProcess>();
    }
    [Obsolete("Method not tested")]
    public static JObject ProcessMethod(this Makerbot.CommunicationConnection cnn, string method, object parameters = null) {
      // parameters = params
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("process_method", new {
        method = method,
        @params = parameters
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ResetToFactory(this Makerbot.CommunicationConnection cnn, bool clearCalibration = false) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("reset_to_factory", new {
        clear_calibration = clearCalibration
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject RunDiagnostics(this Makerbot.CommunicationConnection cnn, object tests) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("run_diagnostics", new {
        tests = tests
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetAutoUnload(this Makerbot.CommunicationConnection cnn, string unloadCase) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_auto_unload", new {
        unload_case = unloadCase
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetToolheadErrorVisibility(this Makerbot.CommunicationConnection cnn, string error, bool ignored) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_toolhead_error_visibility", new {
        error = error,
        ignored = ignored
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetZAdjustedOffset(this Makerbot.CommunicationConnection cnn, double offset) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_z_adjusted_offset", new {
        offset = offset
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetZPauseMm(this Makerbot.CommunicationConnection cnn, int zPauseMm) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_z_pause_mm", new {
        z_pause_mm = zPauseMm
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetupPrinter(this Makerbot.CommunicationConnection cnn, bool jumpToWifiSetup = false) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("setup_printer", new {
        jump_to_wifi_setup = jumpToWifiSetup
      });
      return obj;
    }
    
    /// <summary>
    /// Toggles the sound state of the machine
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <param name="state">Set the state of sound</param>
    /// <returns></returns>
    public static JObject ToggleSound(this Makerbot.CommunicationConnection cnn, bool state) {
      JObject obj = cnn.RawRequest("toggle_sound", new {
        state = state
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject UnloadFilament(this Makerbot.CommunicationConnection cnn, int toolIndex, object temperatureSettings = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("unload_filament", new {
        tool_index = toolIndex,
        temperature_settings = temperatureSettings
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiSetup(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_setup", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject WifiSignalStrength(this Makerbot.CommunicationConnection cnn, string ssid, string iface) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("wifi_signal_strength", new {
        ssid = ssid,
        iface = iface
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject YonkersUpload(this Makerbot.CommunicationConnection cnn, string filepath, string uid, int index, bool? force = null, int? id = null) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("yonkers_upload", new {
        filepath = filepath,
        uid = uid,
        index = index,
        force = force,
        id = id
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ZipLogs(this Makerbot.CommunicationConnection cnn, string zipPath) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("zip_logs", new {
        zip_path = zipPath
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject BirdwingList(this Makerbot.CommunicationConnection cnn, string path) {
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("birdwing_list", new {
        path = path
      });
      return obj;
    }

    /// <summary>
    /// Requests the machine capture an image and save a JPG to the specified onboard location.
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <param name="outputFile">Storage location on the machine. Recommended path '/home/camera/' as it is accessible from the machine's settings.</param>
    /// <returns>Returns true if successfully saved</returns>
    public static bool CaptureImage(this Makerbot.CommunicationConnection cnn, string outputFile) {
      JObject obj = cnn.RawRequest("capture_image", new {
        output_file = outputFile
      });
      if (obj.ContainsKey("result")) {
        return obj["result"].ToObject<bool>();
      } else if (obj.ContainsKey("error")){
        return false;
      }else{
        return false;
      }
    }
    
    /// <summary>
    /// Changes the display name of the machine
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <param name="machineName">New display name for the machine</param>
    /// <returns></returns>
    public static JObject ChangeMachineName(this Makerbot.CommunicationConnection cnn, string machineName) {
      JObject obj = cnn.RawRequest("change_machine_name", new {
        machine_name = machineName
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject DesyncAccount(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("desync_account", null);
      return obj;
    }
    [Obsolete("Format not supported")]
    public static JObject EndCameraStream(this Makerbot.CommunicationConnection cnn) {
      throw new NotSupportedException();
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("end_camera_stream", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ExpireThingiverseCredentials(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("expire_thingiverse_credentials", null);
      return obj;
    }
    
    /// <summary>
    /// Easter Egg
    /// </summary>
    /// <param name="cnn">Machine Connection</param>
    /// <returns></returns>
    public static string FirstContact(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("first_contact", null);
      return obj["result"].ToObject<string>();
    }
    public static JObject GetCloudServicesInfo(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_cloud_services_info", null);
      return obj;
    }
    public static DTOs.RPC.Config GetConfig(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_config", null);
      return obj.ToObject<DTOs.RPC.Config>();
    }
    public static DTOs.RPC.SystemInformation GetSystemInformation(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("get_system_information", null);
      JsonSerializer _writer = new JsonSerializer() {
        NullValueHandling = NullValueHandling.Include
      };
      return obj.ToObject<DTOs.RPC.SystemInformation>(_writer);
      //return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None, _writer);
      //return obj;
    }
    public static JObject Handshake(this Makerbot.CommunicationConnection cnn, string username = null, string hostVersion = null) {
      JObject obj = cnn.RawRequest("handshake", new {
        username = username,
        host_version = hostVersion
      });
      return obj;
    }
    public static DTOs.RPC.NetworkState NetworkState(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("network_state", null);
      return obj.ToObject<DTOs.RPC.NetworkState>();
    }
    public static JObject Ping(this Makerbot.CommunicationConnection cnn) {
      JObject obj = cnn.RawRequest("ping", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject RegisterClientName(this Makerbot.CommunicationConnection cnn, string name) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("register_client_name", new {
        name = name
      });
      return obj;
    }
    [Obsolete("Format not supported")]
    public static JObject RequestCameraFrame(this Makerbot.CommunicationConnection cnn) {
      throw new NotSupportedException();
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("request_camera_frame", null);
      return obj;
    }
    [Obsolete("Format not supported")]
    public static JObject RequestCameraStream(this Makerbot.CommunicationConnection cnn) {
      throw new NotSupportedException();
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("request_camera_stream", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetAnalyticsEnabled(this Makerbot.CommunicationConnection cnn, bool enabled) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_analytics_enabled", new {
        enabled = enabled
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetReflectorEnabled(this Makerbot.CommunicationConnection cnn, bool enabled) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_reflector_enabled", new {
        enabled = enabled
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SetThingiverseCredentials(this Makerbot.CommunicationConnection cnn, string thingiverseUsername, string thingiverseToken) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("set_thingiverse_credentials", new {
        thingiverse_username = thingiverseUsername,
        thingiverse_token = thingiverseToken
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject SyncAccountToBot(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("sync_account_to_bot", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject UpdateAvailableFirmware(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("update_available_firmware", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject GetInit(this Makerbot.CommunicationConnection cnn, string filePath, string fileId) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("get_init", new {
        file_path = filePath,
        file_id = fileId
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject PutInit(this Makerbot.CommunicationConnection cnn, string filePath, string fileId) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("put_init", new {
        file_path = filePath,
        file_id = fileId
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject PutTerm(this Makerbot.CommunicationConnection cnn, string fileId, int length, uint crc) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("put_term", new {
        file_id = fileId,
        length = length,
        crc = crc
      });
      return obj;
    }
    [Obsolete("Method not found")]
    public static JObject GetUniqueIdentifiers(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("get_unique_identifiers", null);
      return obj;
    }
    [Obsolete("Method not found")]
    public static JObject GetSpoolInfo(this Makerbot.CommunicationConnection cnn, int bayIndex) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("get_spool_info", new {
        bay_index = bayIndex
      });
      return obj;
    }
    [Obsolete("Method not found")]
    public static JObject GetTrackedStats(this Makerbot.CommunicationConnection cnn) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("get_tracked_stats", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject PutRaw(this Makerbot.CommunicationConnection cnn, string fileId, char block, object blockSize) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("put_raw", new {
        file_id = fileId,
        block = block,
        block_size = blockSize
      });
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ReflectorAuth(this Makerbot.CommunicationConnection cnn, object authInfo) {
      throw new NotImplementedException();
      JObject obj = cnn.RawRequest("reflector_auth", new {
        auth_info = authInfo
      });
      return obj;
    }

    // JsonRpcNotification

    [Obsolete("Method not tested")]
    public static JObject SystemNotification(this Makerbot.CommunicationConnection cnn, object info) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject StateNotification(this Makerbot.CommunicationConnection cnn, object info) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject ErrorNotification(this Makerbot.CommunicationConnection cnn, int code, int errorId, object source) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject ErrorAcknowledged(this Makerbot.CommunicationConnection cnn, int errorId) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject GetTerm(this Makerbot.CommunicationConnection cnn, string id, uint crc) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not supported")]
    public static JObject CameraFrame(this Makerbot.CommunicationConnection cnn) {
      throw new NotSupportedException();
      //throw new NotImplementedException();
      JObject obj = cnn.RawRequest("camera_frame", null);
      return obj;
    }
    [Obsolete("Method not tested")]
    public static JObject ExtruderChange(this Makerbot.CommunicationConnection cnn, int index, object config) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject NetworkStateChange(this Makerbot.CommunicationConnection cnn, object state) {
      throw new NotImplementedException();
    }
    [Obsolete("Method not tested")]
    public static JObject FirmwareUpdatesInfoChange(this Makerbot.CommunicationConnection cnn, string version, bool updateAvailable, bool isOnline, int error, string releaseDate, string releaseNotes) {
      throw new NotImplementedException();
    }

    // JsonRpcMethod
    [Obsolete("Method not tested")]
    public static JObject GetRaw(this Makerbot.CommunicationConnection cnn, string id, int length) {
      throw new NotImplementedException();
    }
  
    [Obsolete("Method not supported")]
    public static string GetRawCameraImageData(this Makerbot.CommunicationConnection cnn){
      throw new NotSupportedException();
      //throw new NotImplementedException();
      string obj = FCGI.Send(cnn.Host, "camera", new {
        token = cnn.GetAccessToken(Makerbot.CommunicationConnection.AccessTokenContexts.camera)
      });
      return obj;
    }

  }
}