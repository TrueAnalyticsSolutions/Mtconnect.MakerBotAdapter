MakerBot Agent/Adapter Core serves as a data aggregator with MakerBot 3D Printers from the 4th generation and newer. The library provides wrappers for communicating with the machine asynchronously using its JSON-RPC communication. An MTConnect Adapter will be completed and embedded using this library.

## Getting Started
For testing purposes the project is set to *Console Application*, ensure the Output type is set to *Class Library* then build the solution.

Include the *MakerBotAgentAdapterCore* library in your .NET Core application.

### BotManager
Use the *MakerBotAgentAdapterCore.MakerBotAPI.BotManager* class to discover and manage nearby machines.

Initialize the BotManager and let it do the heavy lifting:

```c#
BotManager botManager = new BotManager(autoDiscover: true); // Initialize the bot manager and allow it to go ahead and find machines
foreach(var bot in botManager.Bots){
  Console.WriteLine("{0}\t({1})", bot.Name, bot.Serial);
}
// Example Output:
//   MakerBot+  (0123456789ABCDEFGHIJ)
```

### Makerbot
Use the *MakerBotAgentAdapterCore.MakerBotAPI.Makerbot* class to begin access to a specific machine.

#### Properties
 - *string* **Name**: The Machine Name as defined in the machine's configuration
 - *int* **VID**: Device VID
 - *int* **PID**: Device PID
 - *string* **APIVersion**: Version number of the onboard API received during JSON-RPC handshake
 - *string* **Serial**: Machine serial number
 - *string* **FirmwareVersion**: Version number of the onboard firmware received during JSON-RPC handshake
 - *int* **SSL**: Port number
 - *string* **MotorDriveVersion**
 - *string* **BotType**
 - *string* **MachineType**
 - *CommunicationConnection* **Connection**: Communication wrapper that handles a combination of canned JSON-RPC and FCGI requests
 - *bool* **Connected**: Returns whether a communication connection has been initialized and is currently connected to the machine

#### Methods
 - *-* **Connect**(bool authenticate = false): Open a communication connection. Initializes an instance of *CommunicationConnection*. Allow initialization of connection to hastily run authentication (requires physical interaction with machine first time)
 - *-* **Authorize**: Begins an authorization request with the machine. If this bot has not been saved, then physical interaction with the machine is required.
 - *XmlNode* **AddXml**(ref XmlDocument xDoc): Adds configuration information for this machine to an XmlDocument. Primarily available for BotManager to cache the machine.
 
### CommunicationConnection
The *CommunicationConnection* class serves as a wrapper for individual JSON-RPC commands. At this point many have not been tested and are noted with the Obsolete attribute.

#### Properties
 - *string* **Host**: Host name or IP address of the device
 - *int* **Port**: Communication port, typically 9999
 - *string* **ClientId**: Communication identifier. Default: *MakerWare*
 - *string* **ClientSecret**: Communication identifier secret. Default: I'm not actually going to tell you that, it's a secret.
 - *string* **AuthenticationCode**: Cache of the Authentication Code received from the device.
 - *bool* **IsConnected**: Specifies whether a connection has been established with JSON-RPC on the device

#### Methods
 - *-* **ConnectRPC**: Initializes a connection with JSON-RPC
 - *Newtonsoft.Json.Linq.JObject* **RawRequest**(string method, object parameters = null): CAUTION, use only if you know what you're doing.
 - *string* **GetAccessToken**(CommunicationConnection.AccessTokenContexts ctx): Sends a request to receive an access token for the specified context (*camera*, *jsonrpc*, or *put*)
 - *string* **RunCustomCommand**(string command): Simplified version of RawRequest, typically meant for debugging 'get'-style requests.
 - *-* **RequestAuthentication**: Sends request to device to receive an Authentication Code. Requires physical interaction with machine to complete. Handles like a synchronous request.

### API
Below is an extraction of the available methods (in C#) that help wrap the JSON-RPC methods available. The library utilizes the *ObsoleteAttribute* on each method that has not been tested or not properly supported. Most methods that have not been tested will throw an error as a precautionary.

<details>
  <summary>Click To See API</summary>
<p>
  
  ```c#
  // Method not tested
DesyncAccount(CommunicationConnection cnn)

// Format not supported
EndCameraStream(CommunicationConnection cnn)

// Method not tested
ExpireThingiverseCredentials(CommunicationConnection cnn)

FirstContact(CommunicationConnection cnn)

GetCloudServicesInfo(CommunicationConnection cnn)

GetConfig(CommunicationConnection cnn)

GetSystemInformation(CommunicationConnection cnn)

Handshake(CommunicationConnection cnn, String username = null, String hostVersion = null)

NetworkState(CommunicationConnection cnn)

Ping(CommunicationConnection cnn)

// Method not tested
RegisterClientName(CommunicationConnection cnn, String name)

// Format not supported
RequestCameraFrame(CommunicationConnection cnn)

// Format not supported
RequestCameraStream(CommunicationConnection cnn)

// Method not tested
SetAnalyticsEnabled(CommunicationConnection cnn, Boolean enabled)

// Method not tested
SetReflectorEnabled(CommunicationConnection cnn, Boolean enabled)

// Method not tested
SetThingiverseCredentials(CommunicationConnection cnn, String thingiverseUsername, String thingiverseToken)

// Method not tested
SyncAccountToBot(CommunicationConnection cnn)

// Method not tested
UpdateAvailableFirmware(CommunicationConnection cnn)

// Method not tested
GetInit(CommunicationConnection cnn, String filePath, String fileId)

// Method not tested
PutInit(CommunicationConnection cnn, String filePath, String fileId)

// Method not tested
PutTerm(CommunicationConnection cnn, String fileId, Int32 length, UInt32 crc)

// Method not found
GetUniqueIdentifiers(CommunicationConnection cnn)

// Method not found
GetSpoolInfo(CommunicationConnection cnn, Int32 bayIndex)

// Method not found
GetTrackedStats(CommunicationConnection cnn)

// Method not tested
PutRaw(CommunicationConnection cnn, String fileId, Char block, Object blockSize)

// Method not tested
ReflectorAuth(CommunicationConnection cnn, Object authInfo)

// Method not tested
SystemNotification(CommunicationConnection cnn, Object info)

// Method not tested
StateNotification(CommunicationConnection cnn, Object info)

// Method not tested
ErrorNotification(CommunicationConnection cnn, Int32 code, Int32 errorId, Object source)

// Method not tested
ErrorAcknowledged(CommunicationConnection cnn, Int32 errorId)

// Method not tested
GetTerm(CommunicationConnection cnn, String id, UInt32 crc)

// Method not supported
CameraFrame(CommunicationConnection cnn)

// Method not tested
ExtruderChange(CommunicationConnection cnn, Int32 index, Object config)

// Method not tested
NetworkStateChange(CommunicationConnection cnn, Object state)

// Method not tested
FirmwareUpdatesInfoChange(CommunicationConnection cnn, String version, Boolean updateAvailable, Boolean isOnline, Int32 error, String releaseDate, String releaseNotes)

// Method not tested
GetRaw(CommunicationConnection cnn, String id, Int32 length)

// Method not supported
GetRawCameraImageData(CommunicationConnection cnn)

AddLocalAuth(CommunicationConnection cnn, String username, String localSecret)

// Method not tested
AddMakerbotAccount(CommunicationConnection cnn, String username, String makerbotToken)

// Method not tested
Authenticate(CommunicationConnection cnn, String accessToken)

// Method not tested
Authorize(CommunicationConnection cnn, String username, String makerbotToken = null, String localSecret = null, Boolean chamberBlink = False)

// Method not tested
ClearAuthorize(CommunicationConnection cnn)

// Method not tested
Deauthorize(CommunicationConnection cnn, String username)

GetAuthorized(CommunicationConnection cnn)

// Method not tested
Reauthorize(CommunicationConnection cnn, String username, String makerbotToken = null, String localSecret = null, String localCode = null)

// Method not tested
WifiFreAuthorize(CommunicationConnection cnn, String username, String makerbotToken = null, String localSecret = null)

// Method not tested
GetStaticIpv4(CommunicationConnection cnn, String servicePath)

// Method not tested
SetStaticIpv4(CommunicationConnection cnn, String servicePath, String ip = null, String netmask = null, String gateway = null, Object dns = null, Boolean useStatic = False)

// Method not tested
WifiConnect(CommunicationConnection cnn, String path, String password = null, String name = null)

// Method not tested
WifiDisable(CommunicationConnection cnn)

// Method not tested
WifiDisconnect(CommunicationConnection cnn, String path = null)

WifiEnable(CommunicationConnection cnn)

// Method not tested
WifiForget(CommunicationConnection cnn, String path = null)

// Method not tested
WifiReset(CommunicationConnection cnn)

WifiScan(CommunicationConnection cnn, Boolean forceRescan = False)

// Method not tested
Acknowledged(CommunicationConnection cnn, Int32 errorId)

// Method not tested
AssistedLevel(CommunicationConnection cnn)

BotMaintained(CommunicationConnection cnn)

// Method not tested
BronxUpload(CommunicationConnection cnn, String filepath, Int32 toolhead)

// Method not tested
BrooklynUpload(CommunicationConnection cnn, String filepath, Boolean transferWait = False)

// Method not tested
CalibrateZOffset(CommunicationConnection cnn)

Cancel(CommunicationConnection cnn, Boolean force = False)

// Method not tested
ClearAllZPause(CommunicationConnection cnn)

ClearQueue(CommunicationConnection cnn)

// Method not tested
ClearSshId(CommunicationConnection cnn, String filepath = "")

// Method not tested
ClearZPauseMm(CommunicationConnection cnn, Int32 zPauseMm)

CloseQueue(CommunicationConnection cnn)

Cool(CommunicationConnection cnn, Boolean ignoreToolErrors = False)

CopySshId(CommunicationConnection cnn, String filepath = "")

DisableCheckBuildPlate(CommunicationConnection cnn)

DisableLeds(CommunicationConnection cnn, Boolean disableKnob = False, Boolean disableChamber = False)

DisableZPause(CommunicationConnection cnn)

DownloadAndInstallFirmware(CommunicationConnection cnn)

// Method not tested
DrmPrint(CommunicationConnection cnn, Int32 layoutId)

// Method not tested
DumpMachineConfig(CommunicationConnection cnn, String path)

EnableCheckBuildPlate(CommunicationConnection cnn)

EnableLeds(CommunicationConnection cnn, Boolean enableKnob = False, Boolean enableChamber = False)

EnableZPause(CommunicationConnection cnn)

ExecuteQueue(CommunicationConnection cnn)

ExternalPrint(CommunicationConnection cnn, String url, Boolean ensureBuildPlateClear = True)

// Method not tested
FirmwareCleanup(CommunicationConnection cnn)

GetAvailableZOffsetAdjustment(CommunicationConnection cnn)

GetMachineConfig(CommunicationConnection cnn)

GetPersistentStatistics(CommunicationConnection cnn)

GetPrintHistory(CommunicationConnection cnn)

GetQueueStatus(CommunicationConnection cnn)

GetSoundState(CommunicationConnection cnn)

GetStatistics(CommunicationConnection cnn)

GetToolUsageStats(CommunicationConnection cnn)

GetZAdjustedOffset(CommunicationConnection cnn)

HasZCalibrationRoutine(CommunicationConnection cnn)

// Method not tested
Home(CommunicationConnection cnn, Object axes, Boolean preheat = True)

IsEndstopTriggered(CommunicationConnection cnn)

// Method not tested
LibraryPrint(CommunicationConnection cnn, Int32 layoutId)

// Method not tested
LoadFilament(CommunicationConnection cnn, Int32 toolIndex, Object temperatureSettings = null)

// Method not tested
LoadPrintTool(CommunicationConnection cnn, Int32 index)

// Method not tested
MachineActionCommand(CommunicationConnection cnn, String machineFunc, Object parameters, String name = "", Boolean ignoreToolErrors = False)

// Method not tested
MachineQueryCommand(CommunicationConnection cnn, String machineFunc, Object parameters, Boolean ignoreToolErrors = False)

// Method not tested
MachineQueryProcess(CommunicationConnection cnn, String machineFunc, Object parameters, String name, Boolean ignoreToolErrors = False)

ManualLevel(CommunicationConnection cnn)

OpenQueue(CommunicationConnection cnn, Boolean clear)

// Method not tested
Park(CommunicationConnection cnn)

// Method not tested
Preheat(CommunicationConnection cnn, Object temperatureSettings = null)

Print(CommunicationConnection cnn, String filepath, Boolean ensureBuildPlateClear = True, Boolean transferWait = True)

PrintAgain(CommunicationConnection cnn)

// Method not tested
ProcessMethod(CommunicationConnection cnn, String method, Object parameters = null)

// Method not tested
ResetToFactory(CommunicationConnection cnn, Boolean clearCalibration = False)

// Method not tested
RunDiagnostics(CommunicationConnection cnn, Object tests)

// Method not tested
SetAutoUnload(CommunicationConnection cnn, String unloadCase)

// Method not tested
SetToolheadErrorVisibility(CommunicationConnection cnn, String error, Boolean ignored)

// Method not tested
SetZAdjustedOffset(CommunicationConnection cnn, Double offset)

// Method not tested
SetZPauseMm(CommunicationConnection cnn, Int32 zPauseMm)

// Method not tested
SetupPrinter(CommunicationConnection cnn, Boolean jumpToWifiSetup = False)

ToggleSound(CommunicationConnection cnn, Boolean state)

// Method not tested
UnloadFilament(CommunicationConnection cnn, Int32 toolIndex, Object temperatureSettings = null)

// Method not tested
WifiSetup(CommunicationConnection cnn)

// Method not tested
WifiSignalStrength(CommunicationConnection cnn, String ssid, String iface)

// Method not tested
YonkersUpload(CommunicationConnection cnn, String filepath, String uid, Int32 index, Nullable`1 force = null, Nullable`1 id = null)

// Method not tested
ZipLogs(CommunicationConnection cnn, String zipPath)

// Method not tested
BirdwingList(CommunicationConnection cnn, String path)

CaptureImage(CommunicationConnection cnn, String outputFile)

ChangeMachineName(CommunicationConnection cnn, String machineName)
```

</p>
</details>

