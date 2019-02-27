using System;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public abstract class Debugable {
    public event EventHandler DebugMessage;

    public void write(string msg, ConsoleColor clr = ConsoleColor.White) {
      DebugMessageArgs message = new DebugMessageArgs(msg, clr);
      this.DebugMessage?.Invoke(this, message);
    }
  }
}