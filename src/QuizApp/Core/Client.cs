using Microsoft.AspNetCore.SignalR;
using QuizApp.Common;

namespace QuizApp.Core;

public class CClient {
  public bool Process() {
    if (ClientState == State.CS_DESTROYED) return false;
    UpdateActionState();
    return true;
  }

  public void Send(PKS_DEFAULT packet) {
    ClientProxy.SendAsync("OnPacket", packet);
  }

  private void UpdateActionState() {
    if (QuizSessionID.IsEmpty() || ActionState == State.AS_NONE) {
      curCount = 0;
      NextUpdateTime = 0;
      return;
    }

    if (ActionState == State.AS_LOAD) {
      if (curCount >= Questions.Count) {
        Send(new PKS_END_QUIZ_QUESTION { QuizSessionID = QuizSessionID });
        ActionState = State.AS_END;
        return;
      }

      var question = Questions[curCount];

      Send(
        new PKS_SEND_QUIZ_QUESTION {
          QuizSessionID = QuizSessionID,
          QuestionID = question.QuestionID,
          Content = question.Content,
          Choices = question.Choices,
        }
      );

      ++curCount;
      NextUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (question.MaxResponseTime * 1000);

      ActionState = State.AS_WAIT;

      return;
    }

    if (ActionState == State.AS_WAIT) {
      if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > NextUpdateTime) {
        Send(new PKS_TIMEOUT_QUIZ_QUESTION { QuizSessionID = QuizSessionID });
        ActionState = State.AS_END;
      }

      return;
    }

    if (ActionState == State.AS_END) {
      Send(new PKS_QUIZ_SESSION_STATUS { QuizSessionID = QuizSessionID, Score = Score });
      return;
    }
  }

  public State ClientState { get; set; } = State.CS_NONE;
  public State ActionState { get; set; } = State.AS_NONE;

  public List<QuizQuestion> Questions { get; set; } = [];
  public int Score { get; set; }
  public string QuizSessionID { get; set; }
  public bool IsHosted { get; set; } = false;
  public string UserID { get; set; }
  public string SessionID { get; set; }
  public ISingleClientProxy ClientProxy { get; set; }

  public long NextUpdateTime { get; set; } = 0;
  private int curCount = 0;
}

public class ClientMgr {
  public CClient GetClient(string sessionID) => clients.GetValueOrDefault(sessionID);

  public bool Add(ISingleClientProxy clientProxy, string sessionID) {
    var client = new CClient { SessionID = sessionID, ClientProxy = clientProxy };
    clients.Add(sessionID, client);
    return true;
  }

  public void Update() {
    foreach(var sessionID in clients.Keys) {
      var client = clients[sessionID];
      if (client == null || !client.Process()) clients.Remove(sessionID);
    }
  }

  private readonly Dictionary<string, CClient> clients = [];
}