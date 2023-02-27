using System;
using Newtonsoft.Json;
using MakerBotAgentAdapterCore.MakerBotAPI;
using ConsoulLibrary;

namespace MakerBotAgentAdapterCore {
  public class Program {
    static void Main(string[] args) {
      if (Consoul.Ask("Would you like to run the MTConnect Adapter?")) {
        AdapterInstance adapter = new AdapterInstance(initialPort: 9000, rate: 2000, autoStart: true);
        adapter.AdapterSentChanges += Adapter_AdapterSentChanges;
        while (!Console.KeyAvailable) {
          Console.Clear();

          Consoul.Write("Press any key to stop...", ConsoleColor.Gray);
          System.Threading.Thread.Sleep(30 * 1000);
        }
        adapter.StopAll();
      } else {
        BotManager botManager = new BotManager();
        botManager.DebugMessage += _DebugMessage;
        botManager.Discover(true);
        if (botManager.Bots.Count > 0) {
          Consoul.Write($"Found {botManager.Bots.Count} bots", ConsoleColor.Green);
          foreach (Makerbot bot in botManager.Bots) {
            Consoul.Write($"\t{bot.Name} - {bot.Serial}");
            if (bot.Connection == null) {
              bot.Connect(true);
            }
            bot.Connection.RpcNotificationRelay += Connection_RpcNotificationRelay;
            int opt = -1;
            do {
              opt = Consoul.Prompt("Choose an action", true, new string[] {// Choose("Choose an action",
                "Monitor System Information",
                "Run Custom JSON-RPC Command",
                "Select a Command",
                "Show Authorization Code",
                "Export List of Commands",
                "Listen to Notifications",
                "Exit" });
              switch (opt) {
                case 1:
                  while (!Console.KeyAvailable) {
                    Console.Clear();
                    var si = bot.Connection.GetSystemInformation();
                    Consoul.Write(bot.Name);
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(si);
                    Consoul.Write(string.Format("{0}", data), ConsoleColor.Green);
                    Consoul.Write("Press any key to stop...", ConsoleColor.Gray);
                    System.Threading.Thread.Sleep(1500);
                  }
                  Console.ReadKey(false);
                  opt = -1;
                  break;
                case 2:
                  string command = Consoul.Input("Enter a custom JSON-RPC command:", ConsoleColor.Yellow);
                  object parameters = null;
                  if (Consoul.Ask("Would you like to add parameters to '" + command + "'?")){
                    parameters = Newtonsoft.Json.JsonConvert.DeserializeObject(Consoul.Input("Type JSON parameters as an object"));
                  }
                  string response = Newtonsoft.Json.JsonConvert.SerializeObject(bot.Connection.RawRequest(command, parameters));//.RunCustomCommand(command);
                  Consoul.Write(response, ConsoleColor.Magenta);
                  Console.ReadLine();
                  opt = -1;
                  break;
                case 3:
                  int cmdChoice = -1;
                  string dt = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss tt");
                  string pngLocation = "/home/camera/" + dt + ".jpg";
                  string sshPath = "/home/bot_logs.zip";
                  string birdwingPath = "../";
                  string[] availableCommands = new string[] {
                  "Nevermind, go back",
                  "Get System Information",
                  "Get Config",
                  "Get Raw",
                  "Machine Query Command",
                  "Capture Image (outputFile:" + pngLocation + ")"
                };
                  cmdChoice = Consoul.Prompt("Which command would you like to execute?", true, availableCommands);
                  string cmdResponse = "";
                  switch (cmdChoice) {
                    case 1:
                      continue;
                      break;
                    case 2:
                      var si = bot.Connection.GetSystemInformation();
                      cmdResponse = JsonConvert.SerializeObject(si);
                      break;
                    case 3:
                      cmdResponse = JsonConvert.SerializeObject(bot.Connection.GetConfig());
                      break;
                    case 4:
                      //birdwingPath = Input("Enter Birdwing path");
                      cmdResponse = JsonConvert.SerializeObject(bot.Connection.GetRaw("99", 4048));
                      break;
                    case 5:
                      cmdResponse = JsonConvert.SerializeObject(bot.Connection.MachineQueryCommand("have_run", null));
                      break;
                    case 6:
                      cmdResponse = JsonConvert.SerializeObject(bot.Connection.CaptureImage(pngLocation));
                      break;
                    default:
                      break;
                  }
                  //if (Ask("Would you like to save all of the response data?", ConsoleColor.Yellow)){
                  //  System.IO.File.WriteAllText("Command Responses.csv", output);
                  //}
                  if (!string.IsNullOrEmpty(cmdResponse)) {
                    Consoul.Write(cmdResponse, ConsoleColor.Magenta);
                    Console.ReadLine();
                  }
                  opt = -1;
                  break;
                case 4:
                  Consoul.Write($"Authorization Code: {bot.Connection.AuthenticationCode}");
                  break;
                case 5:
                  Consoul.Write("Croissant Methods");
                  Type t = typeof(MakerBotAPI.CroissantAPI);
                  System.Reflection.MethodInfo[] croissantMethods = t.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                  string strMethod = "";
                  foreach (System.Reflection.MethodInfo croissantMethod in croissantMethods) {
                    Consoul.Write("\t{croissantMethod.Name}");
                    ObsoleteAttribute attr = (ObsoleteAttribute)Attribute.GetCustomAttribute(croissantMethod, typeof(ObsoleteAttribute));
                    if (attr != null) {
                      strMethod += "// " + attr.Message + "\r\n";
                    }
                    strMethod += croissantMethod.Name + "(";
                    System.Reflection.ParameterInfo[] methodParams = croissantMethod.GetParameters();
                    if (methodParams != null) {
                      for (int i = 0; i < methodParams.Length; i++) {
                        System.Reflection.ParameterInfo methodParam = methodParams[i];
                        strMethod += methodParam.ParameterType.Name + " " + methodParam.Name;
                        if (methodParam.HasDefaultValue) {
                          if (methodParam.DefaultValue != null) {
                            if (methodParam.ParameterType.Name == "String") {
                              strMethod += " = \"" + methodParam.DefaultValue.ToString() + "\"";
                            } else {
                              strMethod += " = " + methodParam.DefaultValue.ToString();
                            }
                          } else {
                            strMethod += " = null";
                          }
                        }
                        if (i < methodParams.Length - 1) {
                          strMethod += ", ";
                        }
                      }
                    }
                    strMethod += ")\r\n\r\n";
                  }
                  string croissantMethodsFilePath = "Croissant API Methods.txt";
                  System.IO.File.WriteAllText(croissantMethodsFilePath, strMethod);
                  Consoul.Write($"Saved methods to '{croissantMethodsFilePath}'", ConsoleColor.Green);
                  Consoul.Wait();
                  break;
                case 6:
                  allowNotificationResponses = true;
                  while (!Console.KeyAvailable) {
                    Console.Clear();
                    //var si = bot.Connection.GetSystemInformation();
                    //write(bot.Name);
                    //string data = Newtonsoft.Json.JsonConvert.SerializeObject(si);
                    //write(string.Format("{0}", data), ConsoleColor.Green);
                    Consoul.Write("Press any key to stop...", ConsoleColor.Gray);
                    System.Threading.Thread.Sleep(30 * 1000); // Clear every 30 seconds
                  }
                  allowNotificationResponses = false;
                  break;
                case 7:
                  continue;
                  break;
                default:
                  continue;
                  break;
              }
            } while (opt <= 0);
            //Console.ReadKey();
          }
          if (Consoul.Ask("Would you like to save these machines?")) {
            botManager.Save();
          }
        }
      }


      Consoul.Wait();
    }

    private static void Adapter_AdapterSentChanges(object sender, EventArgs e) {
      SentChangedEventArgs es = e as SentChangedEventArgs;
      Consoul.Write(string.Format("Sent {0} changed values to http://localhost:{1} for bot {2} @{3}", es.ChangedItems.Count.ToString(), es.AdapterPort.ToString(), es.BotSerial, es.Timestamp.ToString()), ConsoleColor.Magenta);
      //throw new NotImplementedException();
    }

    public static bool allowNotificationResponses = false;
    private static void Connection_RpcNotificationRelay(object sender, EventArgs e) {
      if (allowNotificationResponses) {
        MakerBotAPI.Makerbot.CommunicationConnection.RPC.RpcNotificationEventArgs rpcArgs = e as Makerbot.CommunicationConnection.RPC.RpcNotificationEventArgs;
        ConsoleColor clr = ConsoleColor.Magenta;
        if (rpcArgs.method == "system_notification") {
          clr = ConsoleColor.DarkMagenta;
        }else if (rpcArgs.method == "state_notification"){
          clr = ConsoleColor.DarkCyan;
        }else if (rpcArgs.method == "error_notification"){
          clr = ConsoleColor.DarkRed;
        }else{
          clr = ConsoleColor.Magenta;
        }
        Consoul.Write($"Received Notification from RPC Connection Relay: '{rpcArgs.method}'\r\n{JsonConvert.SerializeObject(rpcArgs.rawResponse)}", clr);
      }
      //throw new NotImplementedException();
    }

    private static void _DebugMessage(object sender, EventArgs e) {
      DebugMessageArgs eas = e as DebugMessageArgs;
      Consoul.Write(eas.Message, eas.Color);
      //throw new NotImplementedException();
    }
  }
}
