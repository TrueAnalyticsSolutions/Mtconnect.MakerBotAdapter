
namespace MakerBotAgentAdapterCore.MakerBotAPI.DTOs {
  public class Error {
    public string code;
    public string message;
  }
  public class Firmware_Version {
    public int major;
    public int minor;
    public int bugfix;
    public int build;

    public override string ToString() {
      return string.Format("{0}.{1}.{2}.{3}", this.major, this.minor, this.bugfix, this.build);// base.ToString();
    }
  }
  public class Other {
    public class Broadcast {
      public string commit;
      public string machine_type;
      public string ip;
      public string iserial;
      public string port;
      public Firmware_Version firmware_version;
      public int vid;
      public string builder;
      public int pid;
      public string machine_name;
    }
  }
}
