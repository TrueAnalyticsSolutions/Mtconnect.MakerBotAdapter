namespace MakerBot.Rpc
{
    public class Process : JsonRpcMessage<Process.Result>
    {
        public class Result
        {
            public object methods;
            public object reason;
            public string name;
            public bool cancelled;
            public string step;
            public bool completed;
            public bool cancellable;
            public Error error;
            public string username;
            public int id;
        }
    }
}
