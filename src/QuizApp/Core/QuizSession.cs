using Redis.OM.Modeling;

namespace QuizApp.Core;

[Document(StorageType = StorageType.Json, Prefixes = [nameof(QuizSession)])]
public class QuizSession {
  public const string WAITING = "WAITING";
  public const string PLAYING = "PLAYING";
  public const string CLOSED = "CLOSED";

  [RedisIdField] [Indexed] public string? QuizSessionID { get; set; }
  [Indexed] public string? Name { get; set; }
  [Indexed] public string? Status { get; set; }
  [Indexed] public string? Host { get; set; }
  [Indexed] public List<QuizQuestion> Questions { get; set; }
  [Indexed] public Dictionary<string, ParticipantStatus> Participants { get; set; } = [];
}

public class ParticipantStatus {
  public string UserID { get; set; }
  public int TotalScore { get; set; }
  public long TotalResponseTime { get; set; }
  public Dictionary<string, ParticipantAnswer> Answers { get; set; }
}

public class ParticipantAnswer {
  public string QuestionID { get; set; }
  public string Choice { get; set; }
  public long ResponseTime { get; set; }
  public int Score { get; set; }
}