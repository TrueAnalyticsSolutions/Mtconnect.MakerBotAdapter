using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MakerBot.Rpc
{
    public class MachineConfig : JsonRpcMessage<MachineConfig.Result>
    {
        public class Result
        {
            public Acceleration acceleration;
            public string bot_type;
            public AxesInt build_volume;
            public ExtraSlicerSettings extra_slicer_settings;
            public ExtruderProfiles extruder_profiles;
            public GantryConfiguration gantry_configuration;
            public int makerbot_generation;
            public AxesInt max_speed_mm_per_second;
            public AxesDouble start_position;
            public AxesDouble steps_per_mm;

            public string version;

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
                public AttachedExtruder[] attached_extruders;
                public Dictionary<string, string> supported_extruders;
                public Dictionary<string, ExtruderProfile> extruder_profiles => _additionalData.ToDictionary(o => o.Key, o => o.Value.ToObject<ExtruderProfile>());
                [JsonExtensionData]
                private IDictionary<string, JToken> _additionalData;
                //public ExtruderProfile mk12;
                //public ExtruderProfile mk13;
                //public ExtruderProfile mk13_impla;

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
                    public Dictionary<string, Materials.Material> materials;

                    public class Materials
                    {
                        //public Material pla;
                        //public Material im_pla;

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
                public class ExtruderProfilesConverter : JsonConverter<Dictionary<string, ExtruderProfile>>
                {
                    public override void WriteJson(JsonWriter writer, Dictionary<string, ExtruderProfile> value, JsonSerializer serializer)
                    {
                        throw new NotImplementedException();
                    }

                    public override Dictionary<string, ExtruderProfile> ReadJson(JsonReader reader, Type objectType, Dictionary<string, ExtruderProfile> existingValue, bool hasExistingValue, JsonSerializer serializer)
                    {
                        var profiles = new Dictionary<string, ExtruderProfile>();

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                var propertyName = reader.Value.ToString();
                                var profile = serializer.Deserialize<ExtruderProfile>(reader);
                                profiles.Add(propertyName, profile);
                            }
                        }

                        return profiles;
                    }
                }
            }
        }
    }
}
