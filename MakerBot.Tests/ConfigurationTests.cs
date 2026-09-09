using MakerBot;
using Mtconnect.MakerBotAdapter;
using Xunit;

namespace MakerBot.Tests;

public class ConfigurationTests
{
    [Fact]
    public void MissingFileIsActionable()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Throws<FileNotFoundException>(() => new MakerBotRPCAdapter(path));
    }

    [Fact]
    public void MissingAuthorizationRequiresConfigurator()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"machine\":{\"name\":\"Test\",\"serialNumber\":\"abc\",\"address\":\"10.0.0.61\",\"rpcPort\":9999}}");
            Assert.Throws<MakerBotPairingRequiredException>(() => new MakerBotRPCAdapter(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MalformedJsonIsActionable()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ not-json }");
            var error = Assert.Throws<InvalidDataException>(() => new MakerBotRPCAdapter(path));
            Assert.Contains("not valid JSON", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void InvalidPortIsRejected()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"machine\":{\"name\":\"Test\",\"serialNumber\":\"abc\",\"address\":\"127.0.0.1\",\"rpcPort\":70000,\"authenticationCode\":\"secret\"}}");
            var error = Assert.Throws<InvalidDataException>(() => new MakerBotRPCAdapter(path));
            Assert.Contains("rpcPort", error.Message);
        }
        finally { File.Delete(path); }
    }
}
