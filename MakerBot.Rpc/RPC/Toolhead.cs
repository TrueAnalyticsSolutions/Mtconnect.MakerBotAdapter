namespace MakerBot.Rpc
{
    public class Toolhead
    {
        public Heater heater;
        public int[] locations;
        public string[] axes;
        public string[] type;
        public string program;

        public class Heater
        {
            public int print_temperature_default;
            public int sag_temperature_threshold;
            public int rise_timeout_min;
            public int overrun_temperature_threshold;
            public int at_temp_timeout_min;
            public int fan_preload;
            public int rise_temperature_threshold;
            public int preload_tick_delay;
            public int wait_for_target_timeout;
            public int preheat_temperature;
            public int temperature_freeze_timeout_ms;
        }
    }
}
