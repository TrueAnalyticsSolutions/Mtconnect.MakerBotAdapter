namespace MakerBot.Rpc
{
    public class Handshake : JsonRpcMessage<Handshake.Result>
    {
        public class Result
        {
            public string commit;
            public string machine_type;
            public string ip;
            public string iserial;
            public string port;
            public int vid;
            public int pid;
            public string builder;
            public string machine_name;
            public string api_version;
            public int ssl_port;
            public string motor_driver_version;
            public string bot_type;
        }
    }
}
