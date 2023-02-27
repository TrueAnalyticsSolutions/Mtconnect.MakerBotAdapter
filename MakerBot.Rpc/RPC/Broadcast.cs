namespace MakerBot.Rpc
{
    public class Broadcast
    {
        public string machine_type { get; set; }
        public int vid { get; set; }
        public string ip { get; set; }
        public int pid { get; set; }
        public string api_version { get; set; }
        public string iserial { get; set; }
        public Firmware_Version firmware_version { get; set; }
        public string ssl_port { get; set; }
        public string machine_name { get; set; }
        public string motor_driver_version { get; set; }
        public string bot_type { get; set; }
        public string port;
    }
}
