
namespace MakerBotAgentAdapterCore.MakerBotAPI.DTOs.RPC {
  public class AnswerPending {
    public string answer;
    public string username;
  }
  public class AnswerAccepted {
    public string code;
    public string username;
    public string answer;
  }
  public class Code {
    public string client_id;
    public string username;
    public string status;
    public string answer_code;
  }
  public class Token {
    public string status;
    public string username;
    public string access_token;
  }
  public class TokenFailed {
    public string status;
    public string message;
  }
}
