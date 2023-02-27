namespace MakerBot.Rpc
{
    public interface IJsonRpcNotification
    {
        string jsonrpc { get; set; }

        string method { get; set; }
    }
}
