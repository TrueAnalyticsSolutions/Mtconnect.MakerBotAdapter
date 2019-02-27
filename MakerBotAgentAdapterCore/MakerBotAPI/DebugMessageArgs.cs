using System;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public class DebugMessageArgs : EventArgs {
    public string Message;
    public ConsoleColor Color;

    public DebugMessageArgs(string msg, ConsoleColor clr = ConsoleColor.White) {
      this.Message = msg;
      this.Color = clr;
    }
  }
}