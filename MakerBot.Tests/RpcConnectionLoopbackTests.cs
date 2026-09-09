using MakerBot;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace MakerBot.Tests;

public class RpcConnectionLoopbackTests
{
    [Fact]
    public async Task ConnectsUsingIpv6EndpointAddressFamily()
    {
        if (!Socket.OSSupportsIPv6) return;
        using var listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Start();
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptSocketAsync();
            using var stream = new NetworkStream(socket, ownsSocket: false);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            var request = JObject.Parse((await reader.ReadLineAsync())!);
            var response = Encoding.UTF8.GetBytes(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = request["id"],
                ["result"] = "ipv6-ok"
            }.ToString(Newtonsoft.Json.Formatting.None));
            await stream.WriteAsync(response);
        });

        using var connection = new RpcConnection((IPEndPoint)listener.LocalEndpoint);
        await connection.StartAsync();
        var result = await connection.SendCommand(new RpcRequest("ipv6"));
        Assert.Equal("ipv6-ok", result["result"]?.ToString());
        await server;
    }

    [Fact]
    public async Task CorrelatesOutOfOrderResponsesAndDeliversNotification()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;

        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptSocketAsync();
            using var stream = new NetworkStream(socket, ownsSocket: false);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            var first = JObject.Parse((await reader.ReadLineAsync())!);
            var second = JObject.Parse((await reader.ReadLineAsync())!);
            var bytes = Encoding.UTF8.GetBytes(
                new JObject { ["jsonrpc"] = "2.0", ["method"] = "state_notification", ["params"] = new JObject { ["state"] = "ready" } }.ToString(Newtonsoft.Json.Formatting.None) +
                new JObject { ["jsonrpc"] = "2.0", ["id"] = second["id"], ["result"] = second["method"] }.ToString(Newtonsoft.Json.Formatting.None) +
                new JObject { ["jsonrpc"] = "2.0", ["id"] = first["id"], ["result"] = first["method"] }.ToString(Newtonsoft.Json.Formatting.None));
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
        });

        using var connection = new RpcConnection(endpoint) { RequestTimeout = TimeSpan.FromSeconds(2) };
        var notification = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.OnResponse += message => notification.TrySetResult(message);
        await connection.StartAsync();
        var one = connection.SendCommand(new RpcRequest("first"));
        var two = connection.SendCommand(new RpcRequest("second"));

        Assert.Equal("first", (await one)["result"]?.ToString());
        Assert.Equal("second", (await two)["result"]?.ToString());
        Assert.Equal("state_notification", (await notification.Task.WaitAsync(TimeSpan.FromSeconds(2)))["method"]?.ToString());
        await server;
    }

    [Fact]
    public async Task RequestTimeoutIsBoundedAndDisposeIsIdempotent()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptSocketAsync();
            using var stream = new NetworkStream(socket, ownsSocket: false);
            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync();
            await Task.Delay(500);
        });

        var connection = new RpcConnection((IPEndPoint)listener.LocalEndpoint) { RequestTimeout = TimeSpan.FromMilliseconds(75) };
        await connection.StartAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.SendCommand(new RpcRequest("never_answers")));
        connection.Stop();
        connection.Stop();
        connection.Dispose();
        connection.Dispose();
        await server;
    }

    [Fact]
    public async Task CallerCancellationCancelsPendingRequest()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptSocketAsync();
            using var stream = new NetworkStream(socket, ownsSocket: false);
            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync();
            await Task.Delay(300);
        });

        using var connection = new RpcConnection((IPEndPoint)listener.LocalEndpoint) { RequestTimeout = TimeSpan.FromSeconds(5) };
        await connection.StartAsync();
        using var cancellation = new CancellationTokenSource(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.SendCommand(new RpcRequest("cancel_me"), cancellation.Token));
        await server;
    }
}
