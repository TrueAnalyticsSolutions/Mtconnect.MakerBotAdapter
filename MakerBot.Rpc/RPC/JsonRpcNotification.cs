namespace MakerBot.Rpc
{
    public class JsonRpcNotification<T> : IJsonRpcNotification where T : class
    {
        public string jsonrpc { get; set; }

        public string method { get; set; }

        public T @params {get;set;}
    }
}
