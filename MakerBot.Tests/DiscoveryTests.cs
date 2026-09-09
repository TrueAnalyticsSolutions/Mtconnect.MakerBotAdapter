using MakerBot;
using MakerBot.Rpc;
using Xunit;

namespace MakerBot.Tests;

public class DiscoveryTests
{
    [Fact]
    public void MergeDeduplicatesBySerialAndPrefersIpv4()
    {
        var merged = MachineFactory.MergeDiscoveries(new[]
        {
            new Broadcast { iserial = "SERIAL-A", machine_name = "Printer", ip = "fe80::1234", port = "9999" },
            new Broadcast { iserial = "serial-a", machine_name = "Printer", ip = "10.0.0.61", port = "9999" },
            new Broadcast { iserial = "SERIAL-B", machine_name = "Other", ip = "fe80::5678", port = "9999" }
        });

        Assert.Equal(2, merged.Length);
        Assert.Equal("10.0.0.61", Assert.Single(merged, item => item.iserial.Equals("SERIAL-A", StringComparison.OrdinalIgnoreCase)).ip);
    }
}
