namespace MakerBot.Rpc
{
    public class SystemInformation : JsonRpcMessage<SystemInformation.Result>
    {
        public class Result
        {
            public string machine_type;
            public Toolheads toolheads;
            public string ip;
            public bool has_been_connected_to;
            public string api_version;
            public Firmware_Version firmware_version;
            public CurrentProcess current_process;
            public string machine_name;
            public bool sound;
            public string bot_type;
            public object disabled_errors;
            public string auto_unload;

            public class Toolheads
            {
                public Extruder[] extruder;

                public class Extruder
                {
                    public int current_temperature;
                    public int tool_id;
                    public bool tool_present;
                    public int index;
                    public int error;
                    public int target_temperature;
                    public bool filament_presence;
                    public bool preheating;
                }
            }
            public class CurrentProcess
            {
                public object reason;
                public int thing_id;
                public bool cancelled;
                public int elapsed_time;
                public object start_time;
                public string step;
                public bool cancellable;
                public int progress;
                public bool can_print_again;
                public Error error;
                public string username;
                public int id;
                public string[] methods;
                public string filepath;
                public string name;
                public dynamic print_temperatures;
                public int filament_extruded;
                public int time_remaining;
                public bool complete;
                public double[] extrusion_mass_g;
                public double[] extrusion_distance_mm;
                public string filename;
                public double time_estimation;

            }
        }

    }
}
