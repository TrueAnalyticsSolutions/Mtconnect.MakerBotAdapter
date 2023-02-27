using System;

namespace MakerBot
{
    [Serializable]
    public class MachineConfig
    {
        public string Name { get; set; }

        public string Serial { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public string AuthenticationCode { get; set; }

        public string RpcToken { get; set; }

        public string PutToken { get; set; }

        public string CameraToken { get; set; }

        /// <summary>
        /// Refers to the <c>client_id</c> used when Authenticating.
        /// </summary>
        /// <example>MakerWare</example>
        public string ClientId { get; set; } = "MakerWare";

        /// <summary>
        /// Refers to the <c>client_secret</c> used when Authenticating.
        /// </summary>
        /// <example>MakerBotAgentAdapterCore</example>
        public string ClientSecret { get; set; } = "MakerBotAgentAdapterCore";
    }
}
