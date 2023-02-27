namespace MakerBot.Rpc
{
    public class MachineConfig : JsonRpcMessage<MachineConfig.Result>
    {
        public class Result
        {
            public Acceleration acceleration;
            public string bot_type;
            public GantryConfiguration gantry_configuration;
            public ExtraSlicerSettings extra_slicer_settings;
            public AxesDouble steps_per_mm;
            public AxesDouble start_position;
            public AxesInt max_speed_mm_per_second;
            public int makerbot_generation;
            public AxesInt build_volume;
            public string version;
            public ExtruderProfiles extruder_profiles;

            public class Acceleration
            {
                public int buffer_size;
                public int split_move_recursion_count;
                public AxesDouble min_speed_change_mm_per_s;
                public AxesInt impulse_speed_limit_mm_per_s;
                public AxesInt rate_mm_per_s_sq;
                public double split_move_distance_mm;
                public AxesInt max_speed_change_mm_per_s;
            }
            public class GantryConfiguration
            {
                public int travel_speed_xy;
                public int max_fill_speed;
                public int max_inner_shell_speed;
                public int travel_speed_z;
                public int max_outer_shell_speed;
            }
            public class ExtraSlicerSettings
            {
                public double plate_variability;
            }
            public class ExtruderProfiles
            {
                public ExtruderProfile mk13;
                public ExtruderProfile mk12;
                public AttachedExtruder[] attached_extruders;
                public dynamic supported_extruders;
                public ExtruderProfile mk13_impla;

                public class AttachedExtruder
                {
                    public bool calibrated;
                    public int id;
                }

                public class ExtruderProfile
                {
                    public double nozzle_diameter;
                    public ARate max_speed_mm_per_second;
                    public ARate steps_per_mm;
                    public Materials materials;

                    public class Materials
                    {
                        public Material pla;
                        public Material im_pla;

                        public class Material
                        {
                            public double feed_diameter;
                            public double retract_distance;
                            public int temperature;
                            public int retract_rate;
                            public object acceleration;
                            public double ooze_feedstock_distance;
                            public double max_flow_rate;
                            public int restart_rate;

                            public class MaterialAcceleration
                            {
                                public ARate min_speed_change_mm_per_s;
                                public ARate rate_mm_per_s_sq;
                                public double[][] slip_compensation_table;
                                public ARate max_speed_change_mm_per_s;
                                public ARate impulse_speed_limit_mm_per_s;
                            }
                        }
                    }
                }
            }
        }
    }
}
