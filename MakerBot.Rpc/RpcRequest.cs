using Newtonsoft.Json.Linq;

namespace MakerBot
{
    public class RpcRequest
    {
        public string Method { get; set; }

        public object Parameters { get; set; }

        public RpcRequest(string method, object parameters)
        {
            Method = method;
            Parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RpcRequest)) return false;
            if (Method != (obj as RpcRequest).Method) return false;
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        public JObject ToJsonRpcRequest(int id)
        {
            const string jsonRpcVersion = "2.0";
            
            JObject result = new JObject();
            
            result["jsonrpc"] = jsonRpcVersion;
            result["id"] = id;
            result["method"] = Method;
            
            if (Parameters != null)
            {
                JObject parameters = JObject.FromObject(Parameters);
                result.Add(new JProperty("params", parameters));
            }

            return result;
        }
    }
}
