namespace MakerBot.Rpc
{
    public class WiFiAccessPoints : JsonRpcMessage<WiFiAccessPoints.WiFiAccesPoint[]>
    {
        public class WiFiAccesPoint
        {
            public string password;
            public string path;
            public string name;
            public int strength;
        }
    }
}
