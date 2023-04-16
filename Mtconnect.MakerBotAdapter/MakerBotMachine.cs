using Mtconnect.AdapterInterface.Contracts.Attributes;
using Mtconnect.AdapterInterface.DataItems;
using Mtconnect.AdapterInterface.DataItemTypes;
using Mtconnect.AdapterInterface.DataItemValues;

namespace Mtconnect.MakerBotAdapter
{
    public class MachineAuxiliaries : Auxiliaries
    {
        [DataItemPartial("mdl")]
        public ToolHead ModelExtruder => GetOrAddAuxiliary<ToolHead>(nameof(ModelExtruder));

        [DataItemPartial("spt")]
        public ToolHead SupportExtruder => GetOrAddAuxiliary<ToolHead>(nameof(SupportExtruder));
    }
    public class MachineAxes : Axes
    {
        [DataItemPartial("x")]
        public MachineLinearAxis X => GetOrAddAxis<MachineLinearAxis>(nameof(X));

        [DataItemPartial("y")]
        public MachineLinearAxis Y => GetOrAddAxis<MachineLinearAxis>(nameof(Y));

        [DataItemPartial("z")]
        public MachineLinearAxis Z => GetOrAddAxis<MachineLinearAxis>(nameof(Z));
    }
    public class MachineController : Controller
    {
        [Event("apiv", "indicates the version of the Croissant API")]
        public Application.VERSION ApiVersion { get; set; }

        [Event("firmv", "indicates the version of the firmware")]
        public Firmware.VERSION FirmwareVersion { get; set; }

        [DataItemPartial("p")]
        public MachinePath Path => GetOrAddPath<MachinePath>(nameof(Path));

    }
    public class MachinePath : Path
    {
        [Event("exe", "represents the current execution state of the program on the machine")]
        public override Execution Execution { get; set; }

        [Event("fm", "indicates whether the machine is in PRODUCTION, SETUP, or TEARDOWN depending on the current process state")]
        public FunctionalMode Functionality { get; set; }

        [Event("prg", "reference to the internal filename of the .gcode or .makerbot part program currently executing on the machine")]
        public Program.ACTIVE Program { get; set; }

        [Condition("alrm")]
        public Condition Alarm { get; set; } = new Condition("Alarm");

        [Event("to", "indicates the z-offset defined for this printer")]
        public ToolOffset.LENGTH ToolOffset { get; set; }

        [Event("un", "indicates which user submitted the print job")]
        public OperatorId Username { get; set; }

        [Event("ps", "indicates when a print process started")]
        public ProcessTime.START PrintStart { get; set; }

        [Event("pt", "indicates when a print process is estimated to complete. this is based on the process start time")]
        public ProcessTime.TARGETCOMPLETION EstimatedCompletion { get; set; }

        [Event("pc", "indicates when the print process was completed")]
        public ProcessTime.COMPLETE PrintCompleted { get; set; }

        [Event("state", "indicates the current print process state")]
        public ProcessState State { get; set; }
    }
    public class MachineLinearAxis : Linear
    {
        [Sample("pos")]
        public PathPosition.ACTUAL ActualPosition { get; set; } = null;

        [Sample("cmd")]
        public PathPosition.COMMANDED CommandedPosition { get; set; } = null;
    }
    public class ToolHead : Heating
    {
        [Sample("ttemp", "indicates the target temperature for the extruder in Celsius", Units = "Celsius")]
        public Temperature TargetTemperature { get; set; } = null;

        [Sample("ctemp", "indicates the current, actual temperature for the extruder in Celsius", Units = "Celsius")]
        public Temperature CurrentTemperature { get; set; } = null;

        [Event("tid", "indicates the unique id of the type of extruder installed in the machine (ie. Tough Extruder or Experimental Extruder)")]
        public ToolAssetId ToolId { get; set; } = null;

        [Condition("te", Type = nameof(ConditionTypes.SYSTEM))]
        public Condition ToolError { get; set; } = new Condition("ToolError");

        [Event("fp", "indicates whether the filament in this extruder is depleted or missing")]
        public EndOfBar.PRIMARY OutOfFilament { get; set; }

        // TODO: Add preheating

        // TODO: Add tool_present
    }
    public class MakerBotMachine : IAdapterDataModel
    {
        [Event("a", "Returns AVAILABLE when a socket connection is established with the machine.")]
        public Availability Availability { get; set; }

        [DataItemPartial("ext")]
        public MachineAuxiliaries Auxiliaries { get; set; } = new MachineAuxiliaries();

        [DataItemPartial("ax")]
        public MachineAxes Axes { get; set; } = new MachineAxes();

        [DataItemPartial("ctrl")]
        public MachineController Controller { get; set; } = new MachineController();

        [Event("ip", "represents the network IP address for accessing the machines API")]
        public Network.IPV4ADDRESS IPv4 { get; set; }

        [Event("p", "represents the port number for accessing the machines Croissant API")]
        public NetworkPort Port { get; set; }
    }
}
