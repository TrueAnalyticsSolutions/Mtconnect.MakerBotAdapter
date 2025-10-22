using Mtconnect.AdapterSdk;
using Mtconnect.AdapterSdk.Attributes;
using Mtconnect.AdapterSdk.DataItems;
using Mtconnect.AdapterSdk.DataItemValues;
using System.Collections.Generic;
using System.Linq;
using MtcTypes = Mtconnect.AdapterSdk.DataItemTypes;

namespace Mtconnect.MakerBotAdapter
{
    public class MachineAuxiliaries : MtcTypes.Auxiliaries
    {
        [DataItemPartial("e", "Extruder")]
        public Dictionary<string, ToolHead> Extruders { get; set; } = new Dictionary<string, ToolHead>();
        //[DataItemPartial("mdl", "Regarding the primary, (typically) model printing extruder; ")]
        //public ToolHead ModelExtruder => GetOrAddAuxiliary<ToolHead>(nameof(ModelExtruder));

        //[DataItemPartial("spt", "Regarding the secondary, (typically) support printing extruder; ")]
        //public ToolHead SupportExtruder => GetOrAddAuxiliary<ToolHead>(nameof(SupportExtruder));

        public ToolHead GetOrAddExtruder(string name)
        {
            if (!Extruders.ContainsKey(name))
                Extruders.Add(name, new ToolHead());
            return Extruders[name];
        }

        public override void Unavailable()
        {
            base.Unavailable();

            if (Extruders?.Any() == true)
                foreach (var item in Extruders)
                    item.Value.Unavailable();
        }
    }

    public class MachineAxes : MtcTypes.Axes
    {
        [DataItemPartial("l_")]
        public Dictionary<string, MachineLinearAxis> LinearAxes { get; set; } = new Dictionary<string, MachineLinearAxis>();

        public override void Unavailable()
        {
            base.Unavailable();

            if (LinearAxes?.Any() == true)
                foreach (var item in LinearAxes)
                    item.Value.Unavailable();
        }
    }
    public class MachineController : MtcTypes.Controller
    {
        [Event("apiv", "Version of the Croissant API")]
        public Application.Version ApiVersion { get; set; }

        [Event("firmv", "Version of the firmware")]
        public Firmware.Version FirmwareVersion { get; set; }

        [DataItemPartial("p")]
        public MachinePath Path => GetOrAddPath<MachinePath>(nameof(Path));

        public override void Unavailable()
        {
            base.Unavailable();

            ApiVersion?.Unavailable();
            FirmwareVersion?.Unavailable();
        }
    }
    public class MachinePath : MtcTypes.Path
    {
        [Event("exe", "Current execution state of the program on the machine")]
        public override Execution Execution { get; set; }

        [Event("fm", "Indicates whether the machine is in PRODUCTION, SETUP, or TEARDOWN depending on the current process state")]
        public FunctionalMode Functionality { get; set; }

        [Event("prg", "Reference to the internal filename of the .gcode or .makerbot part program currently executing on the machine")]
        public Program.Active Program { get; set; }

        [Condition("alrm")]
        public Condition Alarm { get; set; } = new Condition("Alarm");

        [Event("to", "z-offset defined for nozzle(s)")]
        public ToolOffset.Length ToolOffset { get; set; }

        [Event("un", "User that submitted the print job")]
        public OperatorId Username { get; set; }

        [Event("ps", "When a print process started")]
        public ProcessTime.Start PrintStart { get; set; }

        [Event("pt", "When a print process is estimated to complete. this is based on the process start time")]
        public ProcessTime.TargetCompletion EstimatedCompletion { get; set; }

        [Event("pc", "When the print process was completed")]
        public ProcessTime.Complete PrintCompleted { get; set; }

        [Event("state", "Current print process state")]
        public ProcessState State { get; set; }

        public override void Unavailable()
        {
            base.Unavailable();

            Execution?.Unavailable();
            Functionality?.Unavailable();
            Program?.Unavailable();
            Alarm?.Unavailable();
            ToolOffset?.Unavailable();
            Username?.Unavailable();
            PrintStart?.Unavailable();
            EstimatedCompletion?.Unavailable();
            PrintCompleted?.Unavailable();
            State?.Unavailable();
        }
    }
    public class MachineLinearAxis : MtcTypes.Linear
    {
        [Sample("pos")]
        public PathPosition.Actual ActualPosition { get; set; }

        [Sample("cmd")]
        public PathPosition.Commanded CommandedPosition { get; set; }

        public override void Unavailable()
        {
            base.Unavailable();

            ActualPosition?.Unavailable();
            CommandedPosition?.Unavailable();
        }
    }
    public class ToolHead : MtcTypes.ExtrusionUnit
    {
        [Sample("ttemp", "Target temperature for the extruder, in Celsius", Units = "CELSIUS")]
        public Temperature TargetTemperature { get; set; }

        [Sample("ctemp", "Actual temperature for the extruder, in Celsius", Units = "CELSIUS")]
        public Temperature CurrentTemperature { get; set; }

        [Event("tid", "MakerBot's internal id for the type of extruder installed in the machine")]
        public ToolAssetId ExtruderId { get; set; }

        [Event("tname", "Model number of the recognized extruder id. Returns UNAVAILABLE when the adapter is unable to recognize the " + nameof(ExtruderId) + ".")]
        public ToolAssetId Extruder { get; set; }

        [Condition("te", Type = nameof(MtcTypes.ConditionTypes.SYSTEM))]
        public Condition ToolError { get; set; } = new Condition("ToolError");

        [Event("fp", "Whether the filament in this extruder is depleted or missing")]
        public EndOfBar.Primary OutOfFilament { get; set; }

        [Event("mat", "The assumed type of material loaded into the extruder based on the capabilities of the extruder. Returns UNAVAILABLE either when undetermined or when " + nameof(OutOfFilament) + " is 'YES'")]
        public Material FilamentType { get; set; }
        // TODO: Add preheating

        // TODO: Add tool_present

        public override void Unavailable()
        {
            base.Unavailable();
            TargetTemperature?.Unavailable();
            CurrentTemperature?.Unavailable();
            ExtruderId?.Unavailable();
            Extruder?.Unavailable();
            ToolError.Unavailable();
            OutOfFilament?.Unavailable();
            FilamentType?.Unavailable();
        }
    }
    public class MakerBotMachine : IAdapterDataModel
    {
        [Event("a", "AVAILABLE when a socket connection is established with the machine.")]
        public Availability Availability { get; set; }

        [DataItemPartial("ext")]
        public MachineAuxiliaries Auxiliaries { get; set; } = new MachineAuxiliaries();

        [DataItemPartial("ax")]
        public MachineAxes Axes { get; set; } = new MachineAxes();

        [DataItemPartial("ctrl")]
        public MachineController Controller { get; set; } = new MachineController();

        [Event("ip", "Network IP address used to access the machine's API")]
        public Network.IPv4Address IPv4 { get; set; }

        [Event("p", "Port number used to access the machine's Croissant API")]
        public NetworkPort Port { get; set; }

        public void Unavailable()
        {
            Availability?.Unavailable();
            Auxiliaries?.Unavailable();
            Axes?.Unavailable();
            Controller?.Unavailable();
            IPv4?.Unavailable();
            Port?.Unavailable();
        }
    }
}
