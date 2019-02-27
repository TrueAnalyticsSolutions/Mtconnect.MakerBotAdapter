using System;
using Newtonsoft.Json;
using MakerBotAgentAdapterCore.MakerBotAPI;

namespace MakerBotAgentAdapterCore {
  public class Program {
    public static void write(string msg, ConsoleColor clr = ConsoleColor.White){
      Console.ForegroundColor = clr;
      Console.WriteLine(msg);
    }
    public static string Input(string message, ConsoleColor color = ConsoleColor.White, bool allowEmpty = false, bool clearConsole = true) {
      string input = "";
      do {
        if (clearConsole) {
          Console.Clear();
        }
        write(message, color);
        input = Console.ReadLine();
        if (allowEmpty) { break; }
      } while (string.IsNullOrEmpty(input));
      return input;
    }
    public static int Choose(string message, params string[] options){
      int choice = -1;
      do {
        Console.Clear();
        write(message, ConsoleColor.Yellow);
        for (int i = 0; i < options.Length; i++) {
          string option = options[i];
          write(string.Format("{0}.{1:-2}", (i + 1).ToString(), option), ConsoleColor.Green);
        }
        if (!int.TryParse(Input("Choose from the options above", clearConsole:false), out choice)){
          choice = -1;
        }
      } while (choice <= 0);
      return choice;
    }
    public static bool Ask(string message, ConsoleColor color = ConsoleColor.White) {
      string input = "null";
      do {
        Console.Clear();
        write(message, color);
        write("Yes = y\t\tNo = n or {Enter}");
        input = Console.ReadLine();
        input = input.ToLower();
        if (string.IsNullOrEmpty(input)) { input = "n"; }
      } while (!(input == "y" || input == "n"));
      return input == "y";
    }
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");
      BotManager botManager = new BotManager();
      botManager.DebugMessage += _DebugMessage;
      botManager.Discover(true);
      if (botManager.Bots.Count > 0){
        write("Found " + botManager.Bots.Count.ToString() + " bots", ConsoleColor.Green);
        foreach (Makerbot bot in botManager.Bots) {
          write("\t" + bot.Name + " - " + bot.Serial);
          if (bot.Connection == null){
            bot.Connect(true);
          }
          int opt = -1;
          do {
            opt = Choose("Choose an action", "Monitor System Information", "Run Custom JSON-RPC Command", "Select a Command", "Show Authorization Code", "Export List of Commands", "Exit");
            switch (opt) {
              case 1:
                //while (!Console.KeyAvailable) {
                //  Console.Clear();
                //  var si = bot.Connection.GetSystemInformation();
                //  write(bot.Name);
                //  string data = Newtonsoft.Json.JsonConvert.SerializeObject(si);
                //  write(string.Format("[{0}]\t{1}", si.id.ToString(), data), ConsoleColor.Green);
                //  write("Press any key to stop...", ConsoleColor.Gray);
                //  System.Threading.Thread.Sleep(1500);
                //}
                //Console.ReadKey(false);
                opt = -1;
                break;
              case 2:
                string command = Input("Enter a custom JSON-RPC command:", ConsoleColor.Yellow);
                string response = bot.Connection.RunCustomCommand(command);
                write(response, ConsoleColor.Magenta);
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
                  "Birdwing List",
                  "Copy Ssh Id (path: " + sshPath + ")",
                  "Capture Image (outputFile:" + pngLocation + ")"
                };
                cmdChoice = Choose("Which command would you like to execute?", availableCommands);
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
                    birdwingPath = Input("Enter Birdwing path");
                    cmdResponse = JsonConvert.SerializeObject(bot.Connection.BirdwingList(birdwingPath));
                    break;
                  case 5:
                    cmdResponse = JsonConvert.SerializeObject(bot.Connection.CopySshId(sshPath));
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
                if (!string.IsNullOrEmpty(cmdResponse)){
                  write(cmdResponse, ConsoleColor.Magenta);
                  Console.ReadLine();
                }
                opt = -1;
                break;
              case 4:
                write("Authorization Code: " + bot.Connection.AuthenticationCode);
                break;
              case 5:
                write("Croissant Methods");
                Type t = typeof(MakerBotAPI.CroissantAPI);
                System.Reflection.MethodInfo[] croissantMethods = t.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                string strMethod = "";
                foreach (System.Reflection.MethodInfo croissantMethod in croissantMethods) {
                  write("\t" + croissantMethod.Name);
                  ObsoleteAttribute attr = (ObsoleteAttribute)Attribute.GetCustomAttribute(croissantMethod, typeof(ObsoleteAttribute));
                  if (attr != null){
                    strMethod += "// " + attr.Message + "\r\n";
                  }
                  strMethod += croissantMethod.Name + "(";
                  System.Reflection.ParameterInfo[] methodParams = croissantMethod.GetParameters();
                  if (methodParams != null){
                    for (int i = 0; i < methodParams.Length; i++) {
                      System.Reflection.ParameterInfo methodParam = methodParams[i];
                      strMethod += methodParam.ParameterType.Name + " " + methodParam.Name;
                      if (methodParam.HasDefaultValue){
                        if (methodParam.DefaultValue != null) {
                          if (methodParam.ParameterType.Name == "String") {
                            strMethod += " = \"" + methodParam.DefaultValue.ToString() + "\"";
                          }else {
                            strMethod += " = " + methodParam.DefaultValue.ToString();
                          }
                        }else {
                          strMethod += " = null";
                        }
                      }
                      if (i < methodParams.Length - 1){
                        strMethod += ", ";
                      }
                    }
                  }
                  strMethod += ")\r\n\r\n";
                }
                string croissantMethodsFilePath = "Croissant API Methods.txt";
                System.IO.File.WriteAllText(croissantMethodsFilePath, strMethod);
                write("Saved methods to '" + croissantMethodsFilePath + "'", ConsoleColor.Green);
                write("Press Enter to continue...");
                Console.ReadLine();
                break;
              case 6:
                continue;
                break;
              default:
                continue;
                break;
            }
          } while (opt<=0);
          //Console.ReadKey();
        }
        if (Ask("Would you like to save these machines?", ConsoleColor.Yellow)){
          botManager.Save();
        }
      }

      write("Press Enter to exit...");
      Console.ReadLine();
    }

    private static void _DebugMessage(object sender, EventArgs e) {
      DebugMessageArgs eas = e as DebugMessageArgs;
      write(eas.Message, eas.Color);
      //throw new NotImplementedException();
    }
  }
}
