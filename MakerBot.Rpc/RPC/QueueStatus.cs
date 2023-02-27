namespace MakerBot.Rpc
{
    public class QueueStatus : JsonRpcMessage<QueueStatus.Result>
    {
        public class Result
        {
            public bool queue_open;
            public object queue;
        }
    }
}
