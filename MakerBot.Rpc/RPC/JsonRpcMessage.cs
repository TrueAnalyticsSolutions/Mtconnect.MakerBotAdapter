namespace MakerBot.Rpc
{
    public class JsonRpcMessage<T> : IJsonRpcMessage where T : class
    {
        public T result { get; set; }

        public string jsonrpc { get; set; }

        public int id { get; set; }
    }
}
