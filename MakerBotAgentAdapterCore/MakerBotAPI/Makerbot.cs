using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public partial class Makerbot : Debugable {
    private string _host;
    private int _port;
    private string _auth;
    private string _cid;// = "MakerWare";
    private string _cs;// = "MakerBotAgentAdapterCore";
    public string Name;
    public int VID;
    public int PID;
    public string APIVersion;
    public string Serial;
    public string FirmwareVersion;
    public int SSL;
    public string MotorDriveVersion;
    public string BotType;
    public string MachineType;

    public CommunicationConnection Connection;

    public bool Connected{
      get{
        return this.Connection != null;
      }
    }

    public Makerbot(XmlNode xMachine) {
      this.Name = xMachine.SelectSingleNode("Name").InnerText;
      this.Serial = xMachine.SelectSingleNode("Serial").InnerText;

      this._host = xMachine.SelectSingleNode("Host").InnerText;
      this._port = int.Parse(xMachine.SelectSingleNode("Port").InnerText);
      this._cid = xMachine.SelectSingleNode("ClientId").InnerText;
      this._cs = xMachine.SelectSingleNode("ClientSecret").InnerText;
      this._auth = xMachine.SelectSingleNode("AuthenticationCode").InnerText;

      this.Connect();
    }
    public Makerbot(DTOs.Other.Broadcast objDiscovered) {

      //JObject rtn = JsonConvert.DeserializeObject<JObject>(rawDiscovery);
      this.Name = objDiscovered.machine_name;// rtn["machine_name"].ToString();
      this.Serial = objDiscovered.iserial;// rtn["iserial"].ToString();
      this._host = objDiscovered.ip;
      this._port = int.Parse(objDiscovered.port);
      this._cid = "MakerWare";
      this._cs = "MakerBotAgentAdapterCore";

    }

    private void Initialize() {
      if (this.Connection.IsConnected) {
        DTOs.RPC.Handshake hs = this.Connection.Handshake().ToObject<DTOs.RPC.Handshake>();

        this.MachineType = hs.result.machine_type;// hs["machine_type"].ToString();
        this.VID = hs.result.vid;// int.Parse(hs["vid"].ToString());
        this.PID = hs.result.pid;// int.Parse(hs["pid"].ToString());
        this.APIVersion = hs.result.api_version;// hs["api_version"].ToString();
        this.Serial = hs.result.iserial;// hs["iserial"].ToString();
                                        //JObject fwv = hs.result.firmware_version.ToArray();// hs["firmware_version"].ToObject<JObject>();
        //this.FirmwareVersion = hs.result.firmware_version.ToString();// string.Format("{0}.{1}.{2}.{3}", fwv["major"].ToString(), fwv["minor"].ToString(), fwv["build"].ToString(), fwv["bugfix"].ToString());
        this.SSL = hs.result.ssl_port;// int.Parse(hs["ssl_port"].ToString());
        this.MotorDriveVersion = hs.result.motor_driver_version;// hs["motor_driver_version"].ToString();
        this.BotType = hs.result.bot_type;// hs["bot_type"].ToString();
      }
    }
    public void Connect(bool authenticate = false){//bool autoAuthenticate = false){
      if (!string.IsNullOrEmpty(this._auth)) {
        this.Connection = new CommunicationConnection(this._host, this._port, this._cid, this._cs, this._auth);
        this.Connection.ConnectRPC();
      } else {
        this.Connection = new CommunicationConnection(this._host, this._port, this._cid, this._cs);
        if (authenticate){
          this.Connection.RequestAuthentication();
        }
      }
      //if (autoAuthenticate) {
      //  this.Connection.ConnectRPC();
      //}

      this.Initialize();


      //if (autoAuthenticate && string.IsNullOrEmpty(this._auth)) {
      //  write("Requesting Authentication...", ConsoleColor.Yellow);
      //  this.Connection.RequestAuthentication();
      //  if (!string.IsNullOrEmpty(this.Connection.AuthenticationCode)){
      //    write("\tReceived Authentication Code", ConsoleColor.Green);
      //    this._auth = this.Connection.AuthenticationCode;
      //  }
      //  write("Requesting RPC Connection...", ConsoleColor.Yellow);
      //  this.Connection.ConnectRPC(this._auth);
      //}
    }
    public void Authorize(){
      this.Connection.RequestAuthentication();
    }

    public XmlNode AddXml(ref XmlDocument xDoc) {
      XmlNode xRoot = xDoc.SelectSingleNode("//Machines").AppendChild(xDoc.CreateElement("Machine"));
      xRoot.AppendChild(xDoc.CreateElement("Name")).InnerText = this.Name;
      xRoot.AppendChild(xDoc.CreateElement("Serial")).InnerText = this.Serial;
      if (this.Connection != null) {
        xRoot.AppendChild(xDoc.CreateElement("Host")).InnerText = this.Connection.Host;
        xRoot.AppendChild(xDoc.CreateElement("Port")).InnerText = this.Connection.Port.ToString();
        xRoot.AppendChild(xDoc.CreateElement("ClientId")).InnerText = this.Connection.ClientId;
        xRoot.AppendChild(xDoc.CreateElement("ClientSecret")).InnerText = this.Connection.ClientSecret;
        xRoot.AppendChild(xDoc.CreateElement("AuthenticationCode")).InnerText = this.Connection.AuthenticationCode;
        xRoot.AppendChild(xDoc.CreateElement("BotType")).InnerText = this.BotType;
      }
      return xRoot;
    }
  }
}