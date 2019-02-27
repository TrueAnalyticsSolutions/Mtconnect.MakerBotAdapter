using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public partial class Makerbot {
    public partial class CommunicationConnection : Debugable {
      public string Host;
      public int Port;
      public string ClientId;
      public string ClientSecret;
      public string AuthenticationCode;
      private string _at_rpc;
      private string _at_put;
      private string _at_cam;

      private RPC _rpc;
      public enum AccessTokenContexts {
        jsonrpc,
        put,
        camera
      }

      public bool IsConnected{
        get{
          return this._rpc != null && this._rpc.IsConnected;
        }
      }

      public CommunicationConnection(string host, int port, string clientId, string clientSecret, bool autoConnect = true) {
        this.Host = host;
        this.Port = port;
        this.ClientId = clientId;
        this.ClientSecret = clientSecret;

        if (autoConnect) {
          this.ConnectRPC();
        }
      }
      public CommunicationConnection(string host, int port, string clientId, string clientSecret, string auth) : this(host, port, clientId, clientSecret, false) {
        this.AuthenticationCode = auth;

        this.ConnectRPC();
      }
      public void ConnectRPC() {
        if (this._rpc == null) {
          this._rpc = new RPC(this.Host, this.Port, this.GetAccessToken(AccessTokenContexts.jsonrpc));
        }
      }
      private void ConnectRPC(string auth) {
        this.AuthenticationCode = auth;
        this.ConnectRPC();
      }

      public JObject RawRequest(string method, object parameters = null){
        return this._rpc.Request(method, parameters);
      }
      public string GetAccessToken(AccessTokenContexts ctx){
        switch (ctx) {
          case AccessTokenContexts.jsonrpc:
            if (string.IsNullOrEmpty(this._at_rpc)){
              this._at_rpc = this._getAccessToken(ctx, this.AuthenticationCode);
            }
            return this._at_rpc;
            break;
          case AccessTokenContexts.put:
            if (string.IsNullOrEmpty(this._at_put)) {
              this._at_put = this._getAccessToken(ctx, this.AuthenticationCode);
            }
            return this._at_put;
            break;
          case AccessTokenContexts.camera:
            if (string.IsNullOrEmpty(this._at_cam)) {
              this._at_cam = this._getAccessToken(ctx, this.AuthenticationCode);
            }
            return this._at_cam;
            break;
          default:
            return "";
            break;
        }
      }

      //public DTOs.FCGI.Handshake DoHandshake() {
      //  JObject response = this._rpc.Request("handshake", new {
      //    username = "conveyor",
      //    host_version = "1.0"
      //  });
      //  if (response.ContainsKey("result")) {
      //    return response.ToObject<DTOs.FCGI.Handshake>();
      //    //return response["result"].ToObject<JObject>();
      //  } else {
      //    write("[Handshake] Response failed: " + JsonConvert.SerializeObject(response), ConsoleColor.Red);
      //  }
      //  return null;
      //}
      //public DTOs.FCGI.SystemInformation GetSystemInformation() {
      //  JObject response = this._rpc.Request("get_system_information");
      //  return response.ToObject<DTOs.FCGI.SystemInformation>();
      //}
      public string RunCustomCommand(string command){
        JObject response = this._rpc.Request(command);
        return JsonConvert.SerializeObject(response);
      }
      public void RequestAuthentication() {
        JObject accessData = JsonConvert.DeserializeObject<JObject>(FCGI.Send(this.Host, "auth", new {
          response_type = "code",
          client_id = this.ClientId,
          client_secret = this.ClientSecret
        }));
        if (accessData != null && accessData.ContainsKey("answer_code")) {
          DateTime limit = DateTime.Now.AddMilliseconds(90 * 1000);// 1min 30sec second time limit, just like MakerBot authentication timer
          while (true) {
            JObject response = JsonConvert.DeserializeObject<JObject>(FCGI.Send(this.Host, "auth", new {
              response_type = "answer",
              client_id = this.ClientId,
              client_secret = this.ClientSecret,
              answer_code = accessData["answer_code"].ToString()
            }));
            string responseData = JsonConvert.SerializeObject(response);
            write("Access Token Response Data: \r\n\t" + responseData, ConsoleColor.Magenta);
            if (response.ContainsKey("answer")) {
              string answerResponse = response["answer"].ToString();
              if (answerResponse == "accepted") {
                this.AuthenticationCode = response["code"].ToString();
                this._rpc.Authenticate(this.AuthenticationCode);
                break;
              } else if (answerResponse == "rejected") {
                throw new UnauthorizedAccessException("Pairing mode is already active for this printer. Press a button on printer to disable pairing mode.");
              } else if (DateTime.Now > limit) {
                throw new TimeoutException();
              }
            }
            System.Threading.Thread.Sleep(1000); // Wait for a second
          }
        } else {
          write("Request for Access Token failed!", ConsoleColor.Red);
        }
      }

      private string _getAccessToken(AccessTokenContexts context, string authCode = "") {
        if (string.IsNullOrEmpty(this.ClientId)) {
          this.ClientId = "MakerWare";
        }
        if (string.IsNullOrEmpty(this.ClientSecret)) {
          this.ClientSecret = "MakerBotAgentAdapterCore";
        }
        if (!string.IsNullOrEmpty(authCode)) {
          this.AuthenticationCode = authCode;
        }
        if (string.IsNullOrEmpty(this.AuthenticationCode)) {
          throw new UnauthorizedAccessException("Authentication Code cannot be null or empty.");
        }
        JObject response = JsonConvert.DeserializeObject<JObject>(FCGI.Send(this.Host, "auth", new {
          response_type = "token",
          client_id = this.ClientId,
          client_secret = this.ClientSecret,
          auth_code = authCode,
          context = context.ToString()
        }));
        if (response != null && response.ContainsKey("status")) {
          if (response["status"].ToString() == "success") {
            return response["access_token"].ToString();
          } else {
            throw new UnauthorizedAccessException();
          }
        } else {
          throw new EntryPointNotFoundException();
        }
      }
    }
  }
}