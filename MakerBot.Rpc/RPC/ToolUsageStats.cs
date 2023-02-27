namespace MakerBot.Rpc
{
    public class ToolUsageStats : JsonRpcMessage<ToolUsageStats.Result>
    {
        public class Result
        {
            public int serial;
            public double extrusion_mass_g;
            public int extrusion_distance_mm;
            public int refurb_count;
            public int extrusion_time_s;
            public int retract_count;
        }
    }
}
