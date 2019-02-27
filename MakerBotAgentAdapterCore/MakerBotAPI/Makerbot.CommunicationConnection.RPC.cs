using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public partial class Makerbot {
    public partial class CommunicationConnection {
      public class RPC : Debugable {
        private string _host;
        private int _port;
        private Socket _cnn;
        private string _token;
        //private string _clientId;
        //private string _clientSecret;
        private int _requestId;
        private int requestId {
          get {
            _requestId++;
            return _requestId;
          }
        }
        private Dictionary<int, JObject> responses;
        private Dictionary<int, JObject> pendingRequests;
        private List<JObject> unsolicitedResponses;
        private System.Threading.Thread _requestThread;

        public bool Authenticated;
        public event EventHandler RpcConnectionChanged;
        public event EventHandler RpcThreadChanged;

        public bool IsConnected {
          get {
            return this._cnn != null && this._cnn.Connected;
          }
        }



        public RPC(string host, int port, string accessToken) {
          this._token = accessToken;
          this._host = host;
          this._port = port;
          //this._clientId = clientId;
          //this._clientSecret = clientSecret;

          this.responses = new Dictionary<int, JObject>();
          this.unsolicitedResponses = new List<JObject>();
          this.pendingRequests = new Dictionary<int, JObject>();
          this._requestId = -1;

          this._cnn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
          //this._cnn.Connect(this._host, this._port);

          this.Start();
          this.Authenticate(accessToken);
        }
        private void Connect() {
          if (!this.IsConnected) {
            this._cnn.Connect(this._host, this._port);
          }
        }
        public void Start() {
          if (!this.IsConnected) {
            write("RPC not connected. Attempting to reconnect...", ConsoleColor.Yellow);
            this.Connect();
          }
          if (this.IsConnected) {
            if (this._requestThread == null) {
              this._requestThread = new System.Threading.Thread(new System.Threading.ThreadStart(this._reader));
            }
            this._requestThread.Start();
          } else {
            throw new Exception("Not connected to RPC socket!");
          }
        }
        public void Stop() {
          if (this._requestThread != null) {
            if (this._requestThread.ThreadState == System.Threading.ThreadState.Running) {
              this._requestThread.Abort();
            }
          }
        }

        public void Authenticate(string accessToken) {
          if (!this.Authenticated) {
            JObject response = this.Request("authenticate", new { access_token = accessToken });
            if (response != null) {
              System.Diagnostics.Debug.WriteLine("RPC Authentication: " + JsonConvert.SerializeObject(response));
              this.Authenticated = true;
            }else{
              throw new NullReferenceException("RPC Authentication Response returned as null");
            }
          }
        }

        public JObject Request(string method, object parameters = null) {
          int requestId = this._request(method, parameters);
          JObject response = this._waitForResponse(requestId);
          if (response != null) {
            if (response.ContainsKey("error")) {
              JObject err = response["error"].ToObject<JObject>();
              int code = int.Parse(err["code"].ToString());
              string message = err["message"].ToString();
              if (code == -32601) {
                write("Not Authenticated!", ConsoleColor.Red);
              } else {
                write("RPC Error [" + code.ToString() + "]: " + message, ConsoleColor.Red);
              }
            } else {
            }
          }
          return response;
        }

        private void _reader() {
          System.Diagnostics.Debug.WriteLine("Starting RPC Reader Thread");
          string buffer = "";
          while (true) {
            byte[] rec = new byte[4096];
            try {
              this._cnn.Receive(rec);
              string response = System.Text.Encoding.UTF8.GetString(rec);
              if (response != null && response.Contains("\u0000")){
                response = response.Replace("\u0000", "");
              }
              buffer += response;
              Tuple<string, string> nextMessage = this._getNextMessage(buffer);
              while (nextMessage.Item1 != null) {
                this._handleResponse(nextMessage.Item1);
                buffer = nextMessage.Item2;
                nextMessage = this._getNextMessage(buffer);
              }
              buffer = nextMessage.Item2;
            } catch (SocketException se) {
              System.Diagnostics.Debug.WriteLine("RPC Error: " + se.ToString());
              write("RPC Error: " + se.ToString(), ConsoleColor.Red);
            }
          }
        }
        private bool _handlingResponse;
        private void _handleResponse(string message) {
          JObject obj = JsonConvert.DeserializeObject<JObject>(message);
          Type t = obj.GetType();
          if (obj.ContainsKey("id")) {
            this._handlingResponse = true;
            int id = int.Parse(obj["id"].ToString());
            if (int.TryParse(obj["id"].ToString(), out id)){// id != null) {
              if (!this.responses.ContainsKey(id)) {
                this.responses.Add(id, obj);
              }
            }else{
              write("Couldn't parse Id of response. Throwing away message: \r\n\t" + message, ConsoleColor.Yellow);
            }
          } else {
            this.unsolicitedResponses.Add(obj);
          }
          this._handlingResponse = false;
        }
        private Tuple<string, string> _getNextMessage(string buffer) {
          string message = null;
          if (buffer.Length == 0) {
            return new Tuple<string, string>(null, buffer);
          }
          int parentIdx = 0;
          int pos = 0;
          foreach (char c in buffer) {
            pos++;
            if (c.ToString() == "{") {
              parentIdx++;
            } else if (c.ToString() == "}") {
              parentIdx--;
              if (parentIdx == 0) {
                message = buffer.Substring(0, pos);
                return new Tuple<string, string>(message, buffer.Substring(pos));
              }
            }
          }
          return new Tuple<string, string>(message, buffer);
        }
        private JObject _waitForResponse(int id, int timeout = 3000) {
          if (this.responses == null){
            this.responses = new Dictionary<int, JObject>();
          }
          if (this != null && id != null && this.responses != null) {
            DateTime limit = DateTime.Now.AddMilliseconds(timeout);
            while (DateTime.Now < limit) {
              if (!this._handlingResponse) {
                if (this.responses.ContainsKey(id) == true) {
                  JObject response = this.responses[id];
                  if (this.pendingRequests.ContainsKey(id)) {
                    this.pendingRequests.Remove(id);
                  }
                  this.responses.Remove(id);
                  return response;
                }
              }else{
                limit.AddMilliseconds(50);
              }
              //System.Threading.Thread.Sleep(50);
            }
          }
          return null;
        }
        private int _request(string method, object parameters = null) {
          //string responseOut = "";
          int requestId = this.requestId;
          JObject joe = new JObject();
          joe["jsonrpc"] = "2.0";
          joe["id"] = requestId.ToString();
          joe["method"] = method;
          if (parameters != null) {
            JObject props = JObject.FromObject(parameters);
            joe.Add(new JProperty("params", props));
          }

          string s = JsonConvert.SerializeObject(joe);
          write(s, ConsoleColor.Yellow);
          byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(s);

          this._cnn.Send(byteArray);
          if (!this.pendingRequests.ContainsKey(requestId)) {
            this.pendingRequests.Add(requestId, joe);
          }
          return requestId;
        }
        

      }
    }
  }
}