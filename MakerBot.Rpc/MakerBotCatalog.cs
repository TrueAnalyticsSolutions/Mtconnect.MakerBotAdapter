using System;
using System.Collections.Generic;
using System.Text;

namespace MakerBot.Rpc
{
    /// <summary>Exact hardware tool identifiers reported in <c>tool_id</c>.</summary>
    public enum MakerBotToolId
    {
        Invalid = 0,
        Mark11 = 1, Mark11Revision1 = 2, Mark11Revision2 = 3, Mark11Revision3 = 4,
        Mark12 = 5, Mark12Revision5 = 6, Mark12Revision1 = 7, Mark13 = 8,
        Mark12Revision2 = 9, Mark12Revision2Point1 = 10, Mark12Revision3 = 11,
        Mark12Revision4 = 12, Mark12Revision6 = 13, Mark13ToughPla = 14,
        Mark13Revision1 = 15, Mark13Revision1ToughPla = 16, Mark13Revision2 = 17,
        Mark13Revision2ToughPla = 18, Mark13Revision3 = 19,
        Mark13Revision3ToughPla = 20, Mark13Revision4 = 21,
        Mark13Revision4ToughPla = 22, Mark13Experimental = 99,
        Mk14Model = 100, Mk14Support = 101, Mk14HotModel = 102, Mk14HotSupport = 103
    }

    /// <summary>
    /// Material capability values used by the generated hardware tool table.
    /// This is not the RPC spool <c>material_type</c> domain.
    /// </summary>
    public enum MakerBotToolMaterial
    {
        Unknown = -1,
        Pla = 0,
        ToughPla = 1,
        Pva = 2
    }

    /// <summary>Material numbers used by RPC spool <c>material_type</c> values.</summary>
    public enum MakerBotSpoolMaterialType
    {
        GenericModel = 0,
        Pla = 1,
        Tough = 2,
        Pva = 3,
        Petg = 4,
        Abs = 5,
        Hips = 6,
        Sr30 = 8,
        Asa = 9
    }

    /// <summary>
    /// Stable MakerBot protocol identifiers recovered from the generated support
    /// tables shipped with MakerBot Print. These values are copied into this
    /// assembly so clients never need MakerBot Print installed at runtime.
    /// </summary>
    public static class MakerBotCatalog
    {
        private static readonly IReadOnlyDictionary<int, MakerBotTool> Tools =
            new Dictionary<int, MakerBotTool>
            {
                [0] = new MakerBotTool(0, "invalid", "Unknown", "unknown", "Unknown"),
                [1] = new MakerBotTool(1, "mk12", "Smart Extruder 11.0", "pla", "PLA"),
                [2] = new MakerBotTool(2, "mk12", "Smart Extruder 11.1", "pla", "PLA"),
                [3] = new MakerBotTool(3, "mk12", "Smart Extruder 11.2", "pla", "PLA"),
                [4] = new MakerBotTool(4, "mk12", "Smart Extruder 11.3", "pla", "PLA"),
                [5] = new MakerBotTool(5, "mk12", "Smart Extruder 12.0", "pla", "PLA"),
                [6] = new MakerBotTool(6, "mk12", "Smart Extruder 12.5", "pla", "PLA"),
                [7] = new MakerBotTool(7, "mk12", "Smart Extruder 12.1", "pla", "PLA"),
                [8] = new MakerBotTool(8, "mk13", "Smart Extruder+", "pla", "PLA"),
                [9] = new MakerBotTool(9, "mk12", "Smart Extruder 12.2", "pla", "PLA"),
                [10] = new MakerBotTool(10, "mk12", "Smart Extruder 12.2.1", "pla", "PLA"),
                [11] = new MakerBotTool(11, "mk12", "Smart Extruder 12.3", "pla", "PLA"),
                [12] = new MakerBotTool(12, "mk12", "Smart Extruder 12.4", "pla", "PLA"),
                [13] = new MakerBotTool(13, "mk12", "Smart Extruder 12.6", "pla", "PLA"),
                [14] = new MakerBotTool(14, "mk13_impla", "Tough PLA Smart Extruder+", "im-pla", "Tough PLA"),
                [15] = new MakerBotTool(15, "mk13", "Smart Extruder+", "pla", "PLA"),
                [16] = new MakerBotTool(16, "mk13_impla", "Tough PLA Smart Extruder+", "im-pla", "Tough PLA"),
                [17] = new MakerBotTool(17, "mk13", "Smart Extruder+", "pla", "PLA"),
                [18] = new MakerBotTool(18, "mk13_impla", "Tough PLA Smart Extruder+", "im-pla", "Tough PLA"),
                [19] = new MakerBotTool(19, "mk13", "Smart Extruder+", "pla", "PLA"),
                [20] = new MakerBotTool(20, "mk13_impla", "Tough PLA Smart Extruder+", "im-pla", "Tough PLA"),
                [21] = new MakerBotTool(21, "mk13", "Smart Extruder+", "pla", "PLA"),
                [22] = new MakerBotTool(22, "mk13_impla", "Tough PLA Smart Extruder+", "im-pla", "Tough PLA"),
                [99] = new MakerBotTool(99, "mk13_experimental", "Experimental Extruder", "pla", "PLA"),
                [100] = new MakerBotTool(100, "mk14", "Model 1 Performance Extruder", "pla", "PLA"),
                [101] = new MakerBotTool(101, "mk14_s", "Support 2 Performance Extruder", "pva", "PVA"),
                [102] = new MakerBotTool(102, "mk14_hot", "Model 1 Performance Extruder", "abs", "ABS"),
                [103] = new MakerBotTool(103, "mk14_hot_s", "Support 2 Performance Extruder", "sr30", "SR-30")
            };

        private static readonly IReadOnlyDictionary<int, string> SpoolMaterials =
            new Dictionary<int, string>
            {
                [0] = "generic_model",
                [1] = "pla",
                [2] = "im-pla",
                [3] = "pva",
                [4] = "pet",
                [5] = "abs",
                [6] = "hips",
                [8] = "sr30",
                [9] = "asa"
            };

        private static readonly IReadOnlyDictionary<string, string> MaterialNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["null"] = "Not Detected",
                ["generic_model"] = "Unknown Material",
                ["pla"] = "PLA",
                ["im-pla"] = "Tough",
                ["pva"] = "PVA",
                ["pet"] = "PETG",
                ["abs"] = "ABS",
                ["hips"] = "HIPS",
                ["sr30"] = "SR-30",
                ["asa"] = "ASA",
                ["nylon"] = "Nylon",
                ["pc-abs"] = "PC-ABS",
                ["pc-abs-fr"] = "PC-ABS-FR",
                ["nylon-cf"] = "Nylon Carbon Fiber",
                ["nylon12-cf"] = "Nylon 12 Carbon Fiber",
                ["im-pla-esd"] = "ESD"
            };

        private static readonly IReadOnlyDictionary<string, string> PrinterNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["mini_8"] = "Replicator Mini+",
                ["replicator_5"] = "Replicator 5th Gen",
                ["replicator_b"] = "Replicator+",
                ["z18_6"] = "Replicator Z18",
                ["fire_e"] = "Method",
                ["lava_f"] = "Method X",
                ["sketch"] = "Sketch"
            };

        /// <summary>Gets metadata for an exact hardware tool identifier.</summary>
        public static bool TryGetTool(int toolId, out MakerBotTool tool) => Tools.TryGetValue(toolId, out tool);

        /// <summary>Returns the canonical material key used by spool RPC responses.</summary>
        public static bool TryGetSpoolMaterial(int materialType, out string material) => SpoolMaterials.TryGetValue(materialType, out material);

        /// <summary>Returns a display name for a canonical material key.</summary>
        public static string GetMaterialName(string material)
        {
            if (string.IsNullOrWhiteSpace(material)) return null;
            return MaterialNames.TryGetValue(material, out var name) ? name : material;
        }

        /// <summary>Returns a display name for an RPC spool material code.</summary>
        public static string GetSpoolMaterialName(int materialType)
        {
            return TryGetSpoolMaterial(materialType, out var material)
                ? GetMaterialName(material)
                : null;
        }

        /// <summary>Returns a display name for a typed RPC spool material value.</summary>
        public static string GetSpoolMaterialName(MakerBotSpoolMaterialType materialType) =>
            GetSpoolMaterialName((int)materialType);

        /// <summary>Returns a display name for a MakerBot <c>bot_type</c>.</summary>
        public static string GetPrinterName(string botType)
        {
            if (string.IsNullOrWhiteSpace(botType)) return null;
            return PrinterNames.TryGetValue(botType, out var name) ? name : botType;
        }

        /// <summary>Returns a readable name for a known toolhead error code.</summary>
        public static string GetToolheadErrorName(int code)
        {
            return Enum.IsDefined(typeof(MakerBotToolheadError), code)
                ? Humanize(((MakerBotToolheadError)code).ToString())
                : null;
        }

        /// <summary>Returns a readable name for a known machine error code.</summary>
        public static string GetMachineErrorName(int code)
        {
            return Enum.IsDefined(typeof(MakerBotMachineError), code)
                ? Humanize(((MakerBotMachineError)code).ToString())
                : null;
        }

        private static string Humanize(string value)
        {
            if (value.StartsWith("k", StringComparison.Ordinal) && value.Length > 1) value = value.Substring(1);
            var result = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1]))) result.Append(' ');
                result.Append(value[i]);
            }

            return result.ToString()
                .Replace("Api", "API")
                .Replace("Eeprom", "EEPROM")
                .Replace("Json", "JSON")
                .Replace("Nfc", "NFC")
                .Replace("Pru", "PRU")
                .Replace("Usb", "USB")
                .Replace("Wifi", "Wi-Fi")
                .Replace("Ssid", "SSID")
                .Replace("Mac", "MAC")
                .Replace("I2 C", "I2C")
                .Replace("Hes", "HES");
        }
    }

    /// <summary>Describes an exact MakerBot hardware tool ID and its default capability.</summary>
    public sealed class MakerBotTool
    {
        internal MakerBotTool(int id, string type, string name, string defaultMaterial, string defaultMaterialName)
        {
            Id = id;
            Type = type;
            Name = name;
            DefaultMaterial = defaultMaterial;
            DefaultMaterialName = defaultMaterialName;
        }

        public int Id { get; }
        public string Type { get; }
        public string Name { get; }
        public string DefaultMaterial { get; }
        public string DefaultMaterialName { get; }
    }

    /// <summary>Toolhead error numbers returned by MakerBot firmware.</summary>
    public enum MakerBotToolheadError
    {
        kNone = 0, kWatchdogTriggered = 11, kInvalidResponse = 12, kNotConnected = 13,
        kCommandIndexMismatch = 14, kChamberNotConnected = 15, kCarriageNotConnected = 16,
        kLidInterlockTriggered = 45, kBadToolConnected = 46, kAcPowerDisconnected = 47,
        kDoorInterlockTriggered = 48, kToolShort = 49, kHeaterShort = 50, kToolFanShort = 51,
        kFilamentFanShort = 52, kMotorOverCurrent = 53, kNoToolConnected = 54,
        kHeaterRiseWatchdogTriggered = 55, kHeaterHoldWatchdogTriggered = 56,
        kHeaterTemperatureSagTriggered = 57, kHeaterTemperatureOverrunTriggered = 58,
        kToolheadTimedOut = 59, kToolheadCommunicationError = 60,
        kThermocoupleCommunicationFailure = 65, kThermocoupleOutOfRange = 66,
        kThermocoupleUnplugged = 67, kThermocoupleTooHot = 68, kThermocoupleDataInvalid = 69,
        kToolOpen = 70, kHeaterOpen = 71, kToolFanOpen = 72, kFilamentFanOpen = 73,
        kHeaterOverTemp = 74, kThermocoupleDataUnchanging = 75, kUnusualStallCondition = 76,
        kMalformedPacket = 78, kInvalidToolIndex = 79, kNoFilament = 80, kFilamentSlip = 81,
        kHesLogFull = 82, kDrawerNoFilament = 83, kUnsupportedTool = 85, kToolReadError = 86,
        kToolChecksumFail = 87, kInvalidEncoderResolution = 88, kCommandTooLong = 89,
        kToolBufferFull = 90, kToolheadBufferFull = 91, kEepromChecksumFailure = 95,
        kChamberThermistorDisconnected = 96, kChamberHeaterDisconnected = 97,
        kChamberHeaterFailure = 98, kChamberFanFailure = 99, kChamberTemperatureOverrun = 100,
        kI2CCommError = 101, kThermocoupleAdcBusy = 129, kMismatchApiVersion = 130,
        kEepromNoSlaveAck = 131, kEepromStartFailure = 132, kEepromSlaveMissedValue = 133,
        kEepromIdVerifyFail = 134, kEepromUnknownVersion = 135, kEepromFatalInternalError = 136,
        kEepromOutOfDate = 137, kBusy = 138, kEmptyEepromCache = 139,
        kEepromWriteFailure = 140, kEepromVerifyFailure = 141, kProgramNotStarted = 142,
        kProgramCompleted = 143, kSeriousInternalError = 144, kNfcScanFailure = 145,
        kNfcReadFailure = 147, kNfcWriteFailure = 148, kNfcUidMismatch = 149
    }

    /// <summary>Machine error numbers returned by MakerBot firmware.</summary>
    public enum MakerBotMachineError
    {
        kOk = 256, kUserConfigNotFound = 257, kUserConfigParseFailure = 258,
        kUserConfigMissingValue = 259, kNoToolheadsDetected = 260,
        kInvalidAccelerationBufferSize = 261, kToolheadMismatchApiVersion = 262,
        kToolheadNoResponse = 263, kJsonToolpathNothingParsed = 264, kInvalidAxis = 265,
        kNotReady = 266, kHeatZeroTemperature = 267, kInvalidHeaterIndex = 268,
        kHeaterAddFailure = 269, kToolheadMalformedPacket = 270, kZeroLengthMove = 271,
        kToolheadNotInitialized = 272, kRemoteFull = 273, kMachineDriverClosed = 274,
        kZPauseValueNotFound = 275, kLocalEmpty = 276, kExtrusionDistanceMissing = 277,
        kInvalidActiveToolSetting = 278, kInvalidToolRequested = 279, kParseMore = 280,
        kBufferFull = 281, kKeepCalling = 282, kInvalidFanDuty = 283,
        kTopBunkFanFailure = 284, kStopIteration = 287, kInterfaceLedCommsError = 288,
        kInvalidProgramPath = 289, kToolProgramFailed = 290, kPowerMonitorI2CFailure = 295,
        kDiagnosticsUnknownStateError = 296, kDiagnosticsTestFailed = 297,
        kDiagnostisUknownTestError = 298, kBadToolCountConfig = 299, kNotImplemented = 300,
        kResumeComplete = 301, kKaitenError = 500, kZPause = 501, kHeaterNotHeating = 504,
        kWifiGeneralError = 900, kWifiSsidRequired = 901, kWifiPasswordInvalid = 902,
        kWifiPasswordRequired = 903, kWifiNoSuchSsid = 904, kWifiEthernetConnected = 905,
        kToolheadNotHeating = 1001, kDefaultConfigNotFound = 1003,
        kDefaultConfigParseFailure = 1004, kDefaultConfigMissingValue = 1005,
        kPruInitializationFailed = 1006, kToolheadCommandTxFailure = 1008,
        kCarriageProgramFailure = 1009, kChamberProgramFailure = 1010,
        kJsonToolpathParseError = 1011, kFileNotFound = 1012, kHomingTimedOut = 1013,
        kPrintToolConnectFailed = 1014, kInvalidEepromFilepath = 1015,
        kHomingNotCompleted = 1016, kToolheadSpiConfigError = 1017,
        kSuspendIndexNotFound = 1018, kSuspendNoValidLastMove = 1019,
        kNoValidHesSlope = 1020, kBadPrintFile = 1021, kHesRebaseFailed = 1022,
        kNoHesLog = 1023, kNoHesChange = 1024, kUnknownHomingMethod = 1025,
        kPowerMonitorAdcFailure = 1026, kBothSidesTooHigh = 1027,
        kLevelingWithFilament = 1028, kNoBuildPlate = 1029, kFileTransferTimeout = 1030,
        kCorruptedFirmwareFile = 1031, kBadHesWaveforms = 1032, kHesLogOverflow = 1033,
        kKnobNotTightened = 1034, kInvalidEndstopType = 1035, kNoMacAddressSet = 1036,
        kToolheadFeederSleepInterrupted = 1037, kCouldNotSendToolheadCommand = 1038,
        kMoveCommandOutsideAxisBounds = 1039, kNoFilamentLoaded = 1040,
        kOutOfFilament = 1041, kPrintingNetworkError = 1042, kCloudSlicingError = 1043,
        kUnsupportedEepromVersion = 1044, kEepromUpdateFailed = 1045,
        kToolNotCalibrated = 1046, kPrintExtruderMismatch = 1048,
        kPrintMachineMismatch = 1049, kPrintVersionMismatch = 1050,
        kInsufficientUsbSpace = 1051, kPrintRaftDisabled = 1052,
        kFirmwareUpdateCheckError = 1053, kFirmwareDownloadError = 1054,
        kInternetConnectionError = 1055, kPrintingUrlError = 1056,
        kRecoverableFilamentJam = 1057, kHesNoContact = 1058, kHesReversed = 1059,
        kLogHesTimedOut = 1060, kNoNfcTagsFound = 1061, kAssistMotorOverTemp = 1070,
        kOperationTimedOut = 1100, kJsonConfigKeyError = 1496,
        kJsonConfigValueError = 1497, kInvalidFileType = 1498, kFileAlreadyOpen = 1499,
        kCriticalKaitenError = 1500, kMachineDriverFailure = 1501
    }
}
