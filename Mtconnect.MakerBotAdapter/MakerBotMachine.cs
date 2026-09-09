using Mtconnect.AdapterSdk;
using Mtconnect.AdapterSdk.Attributes;
using Mtconnect.AdapterSdk.DataItems;
using Mtconnect.AdapterSdk.DataItemTypes;
using Mtconnect.AdapterSdk.DataItemValues;

namespace Mtconnect.MakerBotAdapter
{
    public class Axis : Linear
    {
        [Sample("Pos")]
        public Position.Actual ActualPosition { get; set; } = new Position.Actual(AdapterSdk.Constants.UNAVAILABLE);

        [Sample("Cmd")]
        public Position.Commanded CommandedPosition { get; set; } = new Position.Commanded(AdapterSdk.Constants.UNAVAILABLE);
    }

    public class ToolHead : ExtrusionUnit
    {
        [Sample("TarTemp")]
        public Temperature TargetTemperature { get; set; } = new Temperature(AdapterSdk.Constants.UNAVAILABLE);

        [Sample("CurTemp")]
        public Temperature CurrentTemperature { get; set; } = new Temperature(AdapterSdk.Constants.UNAVAILABLE);

        [Event("ToolNumber", "Raw numeric MakerBot tool_id reported by the toolhead.")]
        public ToolNumber ToolNumber { get; set; } = new ToolNumber(AdapterSdk.Constants.UNAVAILABLE);

        [Event("ToolType", "Canonical MakerBot tool family, such as mk13_impla.")]
        public ToolGroup ToolType { get; set; } = new ToolGroup(AdapterSdk.Constants.UNAVAILABLE);

        [Event("ToolName", "Human-readable name for the attached MakerBot tool ID.")]
        public ToolAssetId ToolName { get; set; } = new ToolAssetId(AdapterSdk.Constants.UNAVAILABLE);

        [Event("ToolMaterial", "Default material capability associated with the attached tool; this is not spool detection.")]
        public Mtconnect.AdapterSdk.DataItemValues.Material ToolMaterial { get; set; } = new Mtconnect.AdapterSdk.DataItemValues.Material(AdapterSdk.Constants.UNAVAILABLE);

        [Condition("ToolError")]
        public Condition ToolError { get; set; } = new Condition("ToolError");
    }

    public class MakerBotMachine : IAdapterDataModel
    {
        [Event("ConnectionStatus", "State of the RPC connection to the MakerBot. Values are CLOSED or ESTABLISHED; this adapter does not listen for inbound machine connections.")]
        public ConnectionStatus ConnectionStatus { get; set; } = new ConnectionStatus(ConnectionStatusValues.CLOSED);

        [Event("Avail", "Returns AVAILABLE when an authenticated socket connection is established with the machine.")]
        public Availability Availability { get; set; } = new Availability(AvailabilityValues.UNAVAILABLE);

        [DataItemPartial("Ext1")]
        public ToolHead Extruder1 { get; set; } = new ToolHead();

        [DataItemPartial("Ext2")]
        public ToolHead Extruder2 { get; set; } = new ToolHead();

        [DataItemPartial("X")]
        public Axis X { get; set; } = new Axis();

        [DataItemPartial("Y")]
        public Axis Y { get; set; } = new Axis();

        [DataItemPartial("Z")]
        public Axis Z { get; set; } = new Axis();

        [Event("Ipv4")]
        public Network.IPv4Address IPv4 { get; set; } = new Network.IPv4Address(AdapterSdk.Constants.UNAVAILABLE);

        [Event("Port")]
        public NetworkPort Port { get; set; } = new NetworkPort(AdapterSdk.Constants.UNAVAILABLE);

        [Event("Exec")]
        public Execution Execution { get; set; } = new Execution(AdapterSdk.Constants.UNAVAILABLE);

        [Event("Prog")]
        public Program.Main Program { get; set; } = new Program.Main(AdapterSdk.Constants.UNAVAILABLE);

        [Event("ProcessOccurrenceId")]
        public ProcessOccurrenceId.Operation ProcessOccurrenceId { get; set; } = new ProcessOccurrenceId.Operation(AdapterSdk.Constants.UNAVAILABLE);

        [Sample("ProcessTimer", "Elapsed time for the current MakerBot process, in seconds.")]
        public ProcessTimer.Process ProcessTimer { get; set; } = new ProcessTimer.Process(AdapterSdk.Constants.UNAVAILABLE);

        [Condition("Alarm")]
        public Condition Alarm { get; set; } = new Condition("Alarm");

        [Event("ToolOffset")]
        public ToolOffset.Length ToolOffset { get; set; } = new ToolOffset.Length(AdapterSdk.Constants.UNAVAILABLE);
    }
}
