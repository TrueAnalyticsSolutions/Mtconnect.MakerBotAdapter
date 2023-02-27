namespace MakerBot.Rpc
{
    public class SystemNotification : JsonRpcNotification<SystemNotification.Result>
    {
        public class Result
        {
            public SystemInformation.Result info { get; set; }
        }
    }
}
