using MakerBot;

namespace Mtconnect.MakerBotAdapter
{
    public sealed class AdapterConfiguration
    {
        public MachineConfig Machine { get; set; }
        public int PollIntervalMilliseconds { get; set; } = 5000;
        public int ReconnectIntervalMilliseconds { get; set; } = 10000;
    }
}
