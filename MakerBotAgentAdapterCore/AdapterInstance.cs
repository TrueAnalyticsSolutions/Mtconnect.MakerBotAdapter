using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using MTConnect;
using Microsoft.Extensions.Configuration;
using System.Timers;

namespace MakerBotAgentAdapterCore {
  public class AdapterInstance {
    private Timer aTimer { get; set; }
    private MakerBotAPI.BotManager BotManager { get; set; }
    public List<MakerbotInstance> Instances { get; set; }
    public event EventHandler AdapterSentChanges;

    public AdapterInstance(int initialPort = 7878, double rate = 500.0, bool autoStart = false) {
      this.Instances = new List<MakerbotInstance>();
      this.BotManager = new MakerBotAPI.BotManager();
      if (rate< 200.0){
        rate = 200.0;//Limit minimum rate
      }else if (rate > 1800000){
        rate = 1800000;//Limit maximum rate
      }

      this.aTimer = new Timer(rate);
      this.aTimer.Elapsed += ATimer_Elapsed;
      int port = initialPort;
      foreach (var bot in this.BotManager.Bots) {
        MakerbotInstance mi = new MakerbotInstance(port, bot);
        mi.SentChanges += MakerbotInstance_SentChanges;
        this.Instances.Add(mi);
        port++;
      }

      if (autoStart){
        this.StartAll();
      }
    }

    private void MakerbotInstance_SentChanges(object sender, EventArgs e) {
      this.AdapterSentChanges?.Invoke(sender, e);
    }

    /// <summary>
    /// Starts the MTConnect Adapter for each connected bot
    /// </summary>
    public void StartAll(){
      foreach (var botInstance in this.Instances.Where(o => !o.Running)) {
        botInstance.Start();
      }
      this.aTimer.Start();
    }
    /// <summary>
    /// Stops the MTConnect Adapter for each connected bot
    /// </summary>
    public void StopAll(){
      this.aTimer.Stop();
      foreach (var botInstance in this.Instances.Where(o => o.Running)) {
        botInstance.Stop();
      }
    }

    private void ATimer_Elapsed(object sender, ElapsedEventArgs e) {
      foreach (MakerbotInstance instance in this.Instances.Where(o => o.Running)) {
        instance.Tick();
      }
      //throw new NotImplementedException();
    }

  }
  public class MakerbotInstance{
    public MakerBotAPI.Makerbot Bot { get; set; }
    public List<ADataItemInstance> DataItems { get; set; }
    public Adapter Adapter { get; set; }
    private List<MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest> Requests { get; set; }
    public bool Running{
      get{
        return this.Adapter != null && this.Adapter.Running;
      }
    }

    public event EventHandler SentChanges;

    public MakerbotInstance(int port) {
      this.Adapter = new Adapter(port, true);
      this.Requests = new List<MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest>();
      this.DataItems = new List<ADataItemInstance>();
    }
    public MakerbotInstance(int port, MakerBotAPI.Makerbot bot):this(port){
      this.Bot = bot;
      this.Bot.Connection.RpcNotificationRelay += Connection_RpcNotificationRelay;

      XmlDocument xDoc = new XmlDocument();
      xDoc.LoadXml(Properties.Resources.CroissantAPIMap);
      XmlNode xBotConfig = xDoc.SelectSingleNode("//Bot[@type='" + this.Bot.BotType + "']");
      if (xBotConfig != null) {
        XmlNodeList xDataItems = xBotConfig.SelectNodes("//DataItem");
        foreach (XmlNode xDataItem in xDataItems) {
          ADataItemInstance aDataItem = null;
          XmlNode xType = xDataItem.SelectSingleNode("RetrievalType");
          if (xType != null) {
            string retrievalType = xType.InnerText.ToLower();
            switch (retrievalType) {
              case "notification":
                aDataItem = new NotificationInstance(this.Adapter, xDataItem);
                break;
              case "request":
                aDataItem = new RequestInstance(this.Adapter, xDataItem);
                MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest rpcReq = (aDataItem as RequestInstance).PrepareRequest();
                if (!this.Requests.Any(o => o.method == rpcReq.method)) {
                  this.Requests.Add(rpcReq);
                }
                break;
              default:
                continue;
                break;
            }
          }
          if (aDataItem != null) {
            this.DataItems.Add(aDataItem);
          }
        }
      }
    }

    public MakerbotInstance(int port, XmlNode xBot):this(port, new MakerBotAPI.Makerbot(xBot)) {
    }

    private void Connection_RpcNotificationRelay(object sender, EventArgs e) {
      if (this.Running) {
        MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcNotificationEventArgs es = e as MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcNotificationEventArgs;
        foreach (ADataItemInstance di in this.DataItems.Where(o => o.RetrievalType == ADataItemInstance.RetrievalTypes.Notification)) {
          (di as NotificationInstance).ProcessResponse(es.method, es.rawResponse);
        }
        if (this.DataItems.Any(o => o.Item.Changed)) {
          this.Adapter.SendChanged();
          this.TriggerSentChangedEvent(new SentChangedEventArgs(this));
        }
      }
    }

    /// <summary>
    /// Starts the MTConnect Adapter for this bot
    /// </summary>
    public void Start(){
      this.Adapter.Start();
    }
    
    /// <summary>
    /// Stops the MTConnect Adapter for this bot
    /// </summary>
    public void Stop(){
      this.Adapter.Stop();
    }

    /// <summary>
    /// Triggers the collection of data for this bot
    /// </summary>
    public void Tick(){
      //List<MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest> requests = new List<MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest>();
      //foreach (ADataItemInstance di in this.DataItems.Where(o => o.RetrievalType == ADataItemInstance.RetrievalTypes.Request)) {
      //  var req = (di as RequestInstance).PrepareRequest();
      //  if (!requests.Contains(req)){
      //    requests.Add(req);
      //  }
      //}
      if (this.Running) {
        foreach (var item in this.Requests) {
          var res = this.Bot.Connection.RawRequest(item.method, item.parameters);
          foreach (ADataItemInstance di in this.DataItems.Where(o => o.RetrievalType == ADataItemInstance.RetrievalTypes.Request)) {
            (di as RequestInstance).ProcessResponse(item.method, res);
          }
        }
        if (this.DataItems.Any(o => o.Item.Changed)) {
          this.Adapter.SendChanged();
          this.TriggerSentChangedEvent(new SentChangedEventArgs(this));
        }
      }
    }
  
    private void TriggerSentChangedEvent(SentChangedEventArgs e){
      this.SentChanges?.Invoke(this, e);
    }
  }
  public class SentChangedEventArgs : EventArgs {
    public int AdapterPort;
    public string BotSerial;
    public DateTime Timestamp;
    public Dictionary<string, string> ChangedItems;

    public SentChangedEventArgs() {
      this.AdapterPort = -1;
      this.BotSerial = "";
      this.Timestamp = DateTime.Now;
      this.ChangedItems = new Dictionary<string, string>();
    }
    public SentChangedEventArgs(MakerbotInstance botInstance) : this() {
      this.AdapterPort = botInstance.Adapter.Port;
      this.BotSerial = botInstance.Bot.Serial;
      this.ChangedItems = botInstance.DataItems.Where(o => o.Item.Changed).ToDictionary(o => o.Name, o => o.Item.Value.ToString());
    }
  }
  public abstract class ADataItemInstance {
    public string Name { get; set; }
    public string JsonPath { get; set; }
    public DataItem Item { get; set; }
    public DataItemTypes Type { get; set; }
    public enum DataItemTypes {
      Condition,
      Event,
      Sample
    }
    public RetrievalTypes RetrievalType { get; set; }
    public enum RetrievalTypes {
      Notification,
      Request
    }
    const string MISSING_XML_CONFIG_PROPERTY = "Xml configuration must contain appropriate properties.";
    const string INVALID_DATA_ITEM_TYPE = "DataItem Type must be Condition, Event, or Sample.";
    const string INVALID_RETRIEVAL_TYPE = "Retrieval Type must be Notification or Request.";

    public ADataItemInstance(Adapter adapter, XmlNode xConfig) {
      XmlNode xProp = xConfig.SelectSingleNode("Id");
      if (xProp != null && !string.IsNullOrEmpty(xProp.InnerText)) {
        this.Name = xProp.InnerText;
      } else {
        throw new NullReferenceException(MISSING_XML_CONFIG_PROPERTY);
      }

      xProp = xConfig.SelectSingleNode("Path");
      if (xProp != null && !string.IsNullOrEmpty(xProp.InnerText)) {
        this.JsonPath = xProp.InnerText;
      } else {
        throw new NullReferenceException(MISSING_XML_CONFIG_PROPERTY);
      }

      xProp = xConfig.SelectSingleNode("Type");
      if (xProp != null && !string.IsNullOrEmpty(xProp.InnerText)) {
        string strType = xProp.InnerText.ToLower();
        if (strType == "condition") {
          this.Type = DataItemTypes.Condition;
        } else if (strType == "event") {
          this.Type = DataItemTypes.Event;
        } else if (strType == "sample") {
          this.Type = DataItemTypes.Sample;
        } else {
          throw new NullReferenceException(INVALID_DATA_ITEM_TYPE);
        }
      } else {
        throw new NullReferenceException(MISSING_XML_CONFIG_PROPERTY);
      }

      this.InitializeItem(adapter);
    }

    public ADataItemInstance(Adapter adapter, string name, string path, DataItemTypes type) {
      if (!string.IsNullOrEmpty(name)) {
        this.Name = name;
      } else {
        throw new ArgumentNullException();
      }
      if (!string.IsNullOrEmpty(path)) {
        this.JsonPath = path;
      } else {
        throw new ArgumentNullException();
      }

      this.InitializeItem(adapter);
    }

    private void InitializeItem(Adapter adapter) {
      switch (this.Type) {
        case DataItemTypes.Condition:
          this.Item = new MTConnect.Condition(this.Name, true);
          break;
        case DataItemTypes.Event:
          this.Item = new MTConnect.Event(this.Name);
          break;
        case DataItemTypes.Sample:
          this.Item = new MTConnect.Sample(this.Name);
          break;
        default:
          this.Item = null;
          break;
      }
      if (this.Item != null) {
        adapter.AddDataItem(this.Item);
        this.Item.Unavailable();
      }
    }
    
    public void ProcessResponse(string method, Newtonsoft.Json.Linq.JObject response){
      throw new NotImplementedException();
    }
  }
  public class NotificationInstance:ADataItemInstance{

    public NotificationInstance(Adapter adapter, XmlNode xConfig):base(adapter, xConfig) {
      this.RetrievalType = RetrievalTypes.Notification;
    }

    public NotificationInstance(Adapter adapter, string name, string path, DataItemTypes type):base(adapter, name, path, type) {
      this.RetrievalType = RetrievalTypes.Notification;
    }
    
    public new void ProcessResponse(string method, Newtonsoft.Json.Linq.JObject response){
      if (this.Item != null){
        Newtonsoft.Json.Linq.JToken jt = response.SelectToken(this.JsonPath);
        if (jt != null){
          this.Item.Value = jt.ToObject<string>();
        }else if (!this.Item.IsUnavailable()){
          this.Item.Unavailable();
        }
      }
    }
  }
  public class RequestInstance:ADataItemInstance {
    public string Method { get; set; }
    public object Parameters { get; set; }

    public RequestInstance(Adapter adapter, XmlNode xConfig) : base(adapter, xConfig) {
      this.RetrievalType = RetrievalTypes.Request;

      XmlNode xProp = xConfig.SelectSingleNode("Method");
      if (xProp != null){
        this.Method = xProp.InnerText;
      }else{
        throw new ArgumentNullException("Specify a JsonRpc method to call for data.");
      }

      xProp = xConfig.SelectSingleNode("Parameters");
      if (xProp != null){
        XmlNodeList xParams = xProp.SelectNodes("Parameter");
        //Dictionary<string, string> dicParams = new Dictionary<string, string>();
        dynamic dynParameters = new { };
        foreach (XmlNode xParam in xParams) {
          string key = "", value = "";
          xProp = xParam.SelectSingleNode("Key");
          if (xProp != null){
            key = xProp.InnerText;
          }else{
            continue;
          }
          if (!string.IsNullOrEmpty(key)){
            //if (!dicParams.ContainsKey(key)) {
            //  dicParams.Add(xProp.InnerText, "");
            //}
            xProp = xParam.SelectSingleNode("Value");
            if (xProp != null){
              value = xProp.InnerText;
            }
            if (!string.IsNullOrEmpty(value)){
              //dicParams[key] = value;
              dynParameters[key] = value;
            }
          }else{
            continue;
          }
        }
        this.Parameters = dynParameters;
      }else{
        this.Parameters = null;
      }

    }

    public RequestInstance(Adapter adapter, string name, string path, DataItemTypes type, string method, object parameters) : base(adapter, name, path, type) {
      this.RetrievalType = RetrievalTypes.Request;
      this.Method = method;
      this.Parameters = parameters;
    }

    public MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest PrepareRequest() {
      if (this.Item != null) {
        MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest req = new MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcRequest(this.Method, this.Parameters);
        return req;
        //Newtonsoft.Json.Linq.JObject response = bot.Connection.RawRequest("", null);
        //if (response != null) {
        //  Newtonsoft.Json.Linq.JToken jt = response.SelectToken(this.JsonPath);
        //  if (jt != null) {
        //    this.Item.Value = jt.ToObject<string>();
        //  } else if (!this.Item.IsUnavailable()) {
        //    this.Item.Unavailable();
        //  }
        //}
      }else{
        return null;
      }
    }
    public new void ProcessResponse(string method, Newtonsoft.Json.Linq.JObject response) {
      if (this.Item != null) {
        if (method == this.Method) {
          Newtonsoft.Json.Linq.JToken jt = response.SelectToken(this.JsonPath);
          if (jt != null) {
            this.Item.Value = jt.ToObject<string>();
          } else if (!this.Item.IsUnavailable()) {
            this.Item.Unavailable();
          }
        }
      }
    }

  }
}
