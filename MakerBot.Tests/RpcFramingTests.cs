using MakerBot;
using System.Text;
using Xunit;

namespace MakerBot.Tests;

public class RpcFramingTests
{
    [Fact]
    public void FragmentedMessageWaitsForCompletion()
    {
        var buffer = new StringBuilder("{\"id\":1,\"result\":{");
        Assert.False(RpcConnection.TryTakeJson(buffer, out _));
        buffer.Append("\"ok\":true}}");
        Assert.True(RpcConnection.TryTakeJson(buffer, out var json));
        Assert.Contains("\"ok\":true", json);
    }

    [Fact]
    public void CombinedMessagesAreSeparatedAndStringBracesAreIgnored()
    {
        var buffer = new StringBuilder("{\"id\":1,\"result\":\"a}b\"}{\"method\":\"notice\"}");
        Assert.True(RpcConnection.TryTakeJson(buffer, out var first));
        Assert.True(RpcConnection.TryTakeJson(buffer, out var second));
        Assert.Contains("a}b", first);
        Assert.Contains("notice", second);
    }
}
