using System;

namespace MakerBot
{
    [Serializable]
    public class MachineConfig
    {
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string Address { get; set; }
        public int RpcPort { get; set; } = 9999;
        public int SslPort { get; set; } = 12309;
        public string AuthenticationCode { get; set; }
        public string RpcToken { get; set; }
        public string PutToken { get; set; }
        public string CameraToken { get; set; }
        public string ClientId { get; set; } = "MakerWare";
        public string ClientSecret { get; set; } = "MakerBotAgentAdapterCore";
        public string MachineType { get; set; }
        public string BotType { get; set; }
        public string FirmwareVersion { get; set; }
    }
}
