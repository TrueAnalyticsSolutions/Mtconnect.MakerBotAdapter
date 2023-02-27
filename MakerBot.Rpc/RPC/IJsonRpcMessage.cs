namespace MakerBot.Rpc
{
    public interface IJsonRpcMessage
    {
        string jsonrpc { get; set; }

        int id { get; set; }
    }
}
