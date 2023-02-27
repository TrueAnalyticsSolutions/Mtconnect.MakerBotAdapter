namespace MakerBot.Rpc
{
    public class MachineActionProcess : JsonRpcMessage<MachineActionProcess.Result>
    {
        public class Result
        {
            public object methods;
            public object @params;
            public object reason;
            public string name;
            public bool cancelled;
            public bool ignore_tool_errors;
            public Error error;
            public string step;
            public bool complete;
            public bool cancellable;
            public string machine_func;
            public string username;
            public int id;

        }
    }
}
