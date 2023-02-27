using Mtconnect.AdapterInterface.Contracts.Attributes;
using Mtconnect.AdapterInterface.DataItems;

namespace Mtconnect.MakerBotAdapter
{
    public class Axis
    {
        [Sample("Pos")]
        public double? ActualPosition { get; set; } = null;

        [Sample("Cmd")]
        public double? CommandedPosition { get; set; } = null;
    }
    public class ToolHead
    {
        [Sample("TarTemp")]
        public int? TargetTemperature { get; set; } = null;

        [Sample("CurTemp")]
        public int? CurrentTemperature { get; set; } = null;

        [Event("ToolId")]
        public string ToolId { get; set; } = null;

        [Condition("ToolError")]
        public Condition ToolError { get; set; } = new Condition("ToolError");

        // TODO: Add filament_presence

        // TODO: Add preheating

        // TODO: Add tool_present

        // TODO: Add index, which I think is position in the head when dealing with multiple toolheads per machine
    }
    public class MakerBotMachine : IAdapterDataModel
    {
        [Event("Avail", "Returns AVAILABLE when a socket connection is established with the machine.")]
        public string Availability { get; set; }

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
        public string IPv4 { get; set; }

        [Event("Port")]
        public int Port { get; set; }

        [Event("Exec")]
        public string Execution { get; set; }

        [Event("Prog")]
        public string Program { get; set; }

        [Condition("Alarm")]
        public Condition Alarm { get; set; } = new Condition("Alarm");

        [Event("ToolOffset")]
        public double? ToolOffset { get; set; }
    }
}
