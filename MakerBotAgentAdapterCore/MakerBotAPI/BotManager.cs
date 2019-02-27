using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
//using System.Web;
using MakerBotAgentAdapterCore;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public class BotManager : Debugable {
    public List<Makerbot> Bots { get; set; }
    private Socket BroadcastChannel;
    private Socket AnswerChannel;
    private XmlDocument xDoc;
    const int _targetPort = 12307;
    const int _listenPort = 12308;
    const int _sourcePort = 12309;

    public BotManager(bool autoDiscover = false) {
      this.Bots = new List<Makerbot>();

      this.BroadcastChannel = _createSocketConnection();
      this.BroadcastChannel.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
      this._connectSocket(this.BroadcastChannel, _sourcePort);
      this.AnswerChannel = _createSocketConnection();
      this.AnswerChannel.Blocking = false;
      this._connectSocket(this.AnswerChannel, _listenPort);

      if (System.IO.File.Exists("Machines.xml")) {
        this.xDoc = new XmlDocument();
        this.xDoc.Load("Machines.xml");
        XmlNodeList xMachines = xDoc.SelectNodes("//Machine");
        foreach (XmlNode xMachine in xMachines) {
          Makerbot bot = new Makerbot(xMachine);
          if (!this.Bots.Any(o => o.Serial == bot.Serial)) {
            this.Bots.Add(bot);
          }
        }
      }

      //this.xDoc.LoadXml()

      if (autoDiscover) {
        this.Discover();
      }
    }
    public void Save(){
      this.xDoc = new XmlDocument();
      this.xDoc.AppendChild(this.xDoc.CreateXmlDeclaration("1.0", "utf-8", "yes"));
      XmlNode xRoot = xDoc.AppendChild(xDoc.CreateElement("Machines"));
      foreach (Makerbot bot in this.Bots) {
        bot.AddXml(ref xDoc);
      }
      this.xDoc.Save("Machines.xml");
    }

    private Socket _createSocketConnection() {
      Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      return s;
    }
    private void _connectSocket(Socket socket, int port) {
      IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
      socket.Bind(remoteEndPoint as EndPoint);
      // socket.Connect(IPAddress.Any, port);
    }

    public Makerbot[] Discover(bool addUponDiscovery = false) {
      List<Makerbot> bots = new List<Makerbot>();
      write("Discovering Printer(s)");
      JObject broadcast = new JObject();
      broadcast["command"] = "broadcast";
      string s = JsonConvert.SerializeObject(broadcast);
      System.Text.Encoding encoder = System.Text.Encoding.UTF8;
      byte[] byteArray = encoder.GetBytes(s);
      IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), _targetPort);
      write("\tBroadcasting...");
      write("\t\t" + s, ConsoleColor.Gray);
      this.BroadcastChannel.SendTo(byteArray, remoteEndPoint);
      int maxAttempts = 2;
      int idx = 0;
      do {
        System.Threading.Thread.Sleep(1000); // Sleep 1 second
        try {
          byte[] recFrom = new byte[1024];
          EndPoint frm = new IPEndPoint(IPAddress.Any, 0);
          int length = this.AnswerChannel.ReceiveFrom(recFrom, ref frm);
          string resp = encoder.GetString(recFrom);
          DTOs.Other.Broadcast objResponse = JsonConvert.DeserializeObject<DTOs.Other.Broadcast>(resp);

          write("\t" + resp, ConsoleColor.Green);
          bots.Add(new Makerbot(objResponse));
        } catch (SocketException se) {
          write("\tRetrying [" + (idx + 1).ToString() + "/" + maxAttempts.ToString() + "]...", ConsoleColor.Yellow);
          if (idx == maxAttempts - 1) {
            write("\t" + se.ToString(), ConsoleColor.Yellow);
          }
        } catch (Exception ex) {
          write("\tError: " + ex.ToString(), ConsoleColor.Red);
        }
        idx++;
      } while (idx < maxAttempts);
      if (idx >= maxAttempts) {
        //write("\tTimed Out!", ConsoleColor.Red);
      }
      if (addUponDiscovery){
        foreach (Makerbot bot in bots) {
          if (!this.Bots.Any(o => o.Serial == bot.Serial)) {
            this.Bots.Add(bot);
          }
        }
      }
      return bots.ToArray();
    }

  }
}