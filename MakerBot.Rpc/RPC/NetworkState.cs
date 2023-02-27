namespace MakerBot.Rpc
{
    public class NetworkState : JsonRpcMessage<NetworkState.Result>
    {
        public class Result
        {
            public string gateway;
            public bool tethering;
            public string ip;
            public string wifi;
            public bool @static;
            public string[] dns;
            public string tether_name;
            public string state;
            public string netmask;
            public string service_hash;
            public string name;
        }
    }
}
