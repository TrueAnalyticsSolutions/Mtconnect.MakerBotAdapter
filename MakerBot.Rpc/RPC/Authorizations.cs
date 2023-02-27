using System.Net;

namespace MakerBot.Rpc
{
    public class Authorizations : JsonRpcMessage<Authorization[]>
    {
        public class Authorization
        {
            public int local_auth_count;
            public string username;
            public bool makerbot_account;
        }
    }
}
