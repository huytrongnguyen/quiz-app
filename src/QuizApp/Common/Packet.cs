using QuizApp.Core;

namespace QuizApp.Common;

public abstract class PKS_DEFAULT {
  public virtual string Command { get; }
}

public class PKS_MESSAGE : PKS_DEFAULT {
  public const string LEVEL_ERROR = "ERROR";

  public override string Command { get; } = "PACKET_MESSAGE";
  public string Level { get; set; }
  public string Message { get; set; }
}

public class PKS_CREATE_QUIZ_SESSION_OK : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_CREATE_QUIZ_SESSION_OK";
  public string QuizSessionID { get; set; }
}

public class PKS_CLOSE_QUIZ_SESSION_OK : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_CLOSE_QUIZ_SESSION_OK";
  public string QuizSessionID { get; set; }
}

public class PKS_JOIN_QUIZ_SESSION_OK : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_JOIN_QUIZ_SESSION_OK";
  public string QuizSessionID { get; set; }
  public List<string> Participants { get; set; }
}

public class PKS_START_QUIZ_SESSION : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_START_QUIZ_SESSION";
  public string QuizSessionID { get; set; }
  public List<string> Participants { get; set; }
}

public class PKS_SEND_QUIZ_QUESTION : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_SEND_QUIZ_QUESTION";
  public string QuizSessionID { get; set; }
  public string QuestionID { get; set; }
  public string Content { get; set; }
  public List<string> Choices { get; set; }
}

public class PKS_TIMEOUT_QUIZ_QUESTION : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_TIMEOUT_QUIZ_QUESTION";
  public string QuizSessionID { get; set; }
}

public class PKS_END_QUIZ_QUESTION : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_END_QUIZ_QUESTION";
  public string QuizSessionID { get; set; }
}

public class PKS_QUIZ_SESSION_STATUS : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_END_QUIZ_QUESTION";
  public string QuizSessionID { get; set; }
  public int Score { get; set; }
}

public class PKS_QUIZ_SESSION_LEADERBOARD : PKS_DEFAULT {
  public override string Command { get; } = "PACKET_QUIZ_SESSION_LEADERBOARD";
  public string QuizSessionID { get; set; }
  public List<ParticipantStatus> Leaderboard { get; set; }
}