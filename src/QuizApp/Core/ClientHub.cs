using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizApp.Common;
using Redis.OM;
using Redis.OM.Searching;

namespace QuizApp.Core;

public class ClientHub(ClientMgr _clientMgr, RedisConnectionProvider _provider, QuizDbContext _dbContext) : Hub {
  public override Task OnConnectedAsync() {
    clientMgr.Add(Clients.Caller, Context.ConnectionId);
    return base.OnConnectedAsync();
  }

  public override Task OnDisconnectedAsync(Exception? exception) {
    var client = clientMgr.GetClient(Context.ConnectionId);
    if (client != null) client.ClientState = State.CS_DESTROYED;
    return base.OnDisconnectedAsync(exception);
  }

  public void OnPacket(Dictionary<string, object> p) {
    var client = clientMgr.GetClient(Context.ConnectionId);

    var command = p.GetValueOrDefault("command")?.ToString();

    switch (command) {
      case "PACKET_JOIN_QUIZ_SESSION": OnJoinQuizSession(p, client); break;
      case "PACKET_CREATE_QUIZ_SESSION": OnCreateQuizSession(p, client); break;
      case "PACKET_START_QUIZ_SESSION": OnStartQuizSession(p, client); break;
      case "PACKET_CONTINUE_QUIZ_SESSION": OnContinueQuizSession(p, client); break;
      case "PACKET_ANSWER_QUIZ_QUESTION": OnAnswerQuizQuestion(p, client); break;
      case "PACKET_GET_QUIZ_SESSION_LEADERBOARD": OnGetQuizSessionLeaderboard(p, client); break;
      case "PACKET_CLOSE_QUIZ_SESSION": OnCloseQuizSession(p, client); break;
    }
  }

  private async void OnJoinQuizSession(Dictionary<string, object> p, CClient client) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.WAITING );
    if (quizSession == null) {
      client.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    var participant = client.SessionID;
    if (quizSession.Participants.ContainsKey(participant)) {
      client.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "You are already joined." });
      return;
    }

    var userID = p.GetValueOrDefault("userID")?.ToString();
    if (userID.IsEmpty()) {
      client.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "UserID is null." });
      return;
    }

    quizSession.Participants.Add(participant, new ParticipantStatus { UserID = userID });
    await quizSessionDataSet.SaveAsync();

    client.QuizSessionID = quizSessionID;
    client.UserID = userID;
    client.Send(new PKS_JOIN_QUIZ_SESSION_OK { QuizSessionID = quizSessionID, Participants = quizSession.Participants.Values.Select(x => x.UserID).ToList() });
  }

  private async void OnCreateQuizSession(Dictionary<string, object> p, CClient client) {
    var name = p.GetValueOrDefault("name")?.ToString();
    var questionIDs = p.GetValueOrDefault("questions")?.ToString().Split(",");
    var questions = quizQuestionDbSet.Where(x => questionIDs.Contains(x.QuestionID)).ToList();
    questions.ForEach(question => question.ShuffleChoice());

    var hostID = p.GetValueOrDefault("userID")?.ToString();

    var quizSessionID = await quizSessionDataSet.InsertAsync(
      new QuizSession {
        Name = name,
        Status = QuizSession.WAITING,
        Host = hostID,
        Questions = questions
      }
    );

    client.QuizSessionID = quizSessionID;
    client.UserID = hostID;
    client.IsHosted = true;
    client.Send(new PKS_CREATE_QUIZ_SESSION_OK { QuizSessionID = quizSessionID });
  }

  private async void OnStartQuizSession(Dictionary<string, object> p, CClient hostClient) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.WAITING );
    if (quizSession == null) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    if (quizSession.Host != hostClient.UserID) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Only host can start." });
      return;
    }

    quizSession.Status = QuizSession.PLAYING;
    await quizSessionDataSet.SaveAsync();

    var clients = new List<CClient> { hostClient };
    foreach(var sessionID in quizSession.Participants.Keys) {
      var participantClient = clientMgr.GetClient(sessionID);
      if (participantClient != null) clients.Add(participantClient);
    }

    clients.ForEach(client => {
      client.Send(new PKS_START_QUIZ_SESSION { QuizSessionID = quizSession.QuizSessionID, Participants = quizSession.Participants.Values.Select(x => x.UserID).ToList() });
      client.ActionState = State.AS_LOAD;
      client.Questions = quizSession.Questions;
    });
  }

  private async void OnContinueQuizSession(Dictionary<string, object> p, CClient hostClient) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.PLAYING );
    if (quizSession == null) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    if (quizSession.Host != hostClient.SessionID) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Only host can continue." });
      return;
    }

    var clients = new List<CClient> { hostClient };
    foreach(var sessionID in quizSession.Participants.Keys) {
      var participantClient = clientMgr.GetClient(sessionID);
      if (participantClient != null) clients.Add(participantClient);
    }

    clients.ForEach(client => {
      client.ActionState = State.AS_LOAD;
      client.Questions = quizSession.Questions;
    });
  }

  private async void OnAnswerQuizQuestion(Dictionary<string, object> p, CClient participantClient) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.PLAYING );
    if (quizSession == null) {
      participantClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    var participant = quizSession.Participants.GetValueOrDefault(participantClient.SessionID);
    if (participant == null) {
      participantClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "User does not exitst in this session." });
      return;
    }

    var questionID = p.GetValueOrDefault("questionID")?.ToString();
    var question = quizSession.Questions.FirstOrDefault(x => x.QuestionID == questionID);
    if (question == null) {
      participantClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Question does not exist in this session." });
      return;
    }

    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var remainTime = participantClient.NextUpdateTime - currentTime;
    if (remainTime <= 0) {
      participantClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Timeout." });
      return;
    }

    var userChoice = p.GetValueOrDefault("choice")?.ToString();
    var participantAnswer = new ParticipantAnswer {
      QuestionID = questionID,
      Choice = userChoice,
      ResponseTime = question.MaxResponseTime - remainTime,
      Score = userChoice == question.CorrectChoice ? 1 : 0,
    };

    participant.Answers.Add(questionID, participantAnswer);
    participant.TotalScore += participantAnswer.Score;
    participant.TotalResponseTime += participantAnswer.ResponseTime;

    await quizSessionDataSet.SaveAsync();

    participantClient.Score = participant.TotalScore;
  }

  private async void OnGetQuizSessionLeaderboard(Dictionary<string, object> p, CClient client) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.PLAYING );
    if (quizSession == null) {
      client.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    client.Send(
      new PKS_QUIZ_SESSION_LEADERBOARD {
        QuizSessionID = quizSessionID,
        Leaderboard = quizSession.Participants.Values.OrderByDescending(x => x.TotalScore).ThenBy(x => x.TotalResponseTime).ToList(),
      }
    );
  }

  private async void OnCloseQuizSession(Dictionary<string, object> p, CClient hostClient) {
    var quizSessionID = p.GetValueOrDefault("quizSessionID")?.ToString();
    var quizSession = await quizSessionDataSet.FirstOrDefaultAsync(x => x.QuizSessionID == quizSessionID && x.Status == QuizSession.PLAYING );
    if (quizSession == null) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Session does not exitst." });
      return;
    }

    if (quizSession.Host != hostClient.UserID) {
      hostClient.Send(new PKS_MESSAGE { Level = PKS_MESSAGE.LEVEL_ERROR, Message = "Only host can close." });
      return;
    }

    quizSession.Status = QuizSession.CLOSED;
    await quizSessionDataSet.SaveAsync();

    var clients = new List<CClient> { hostClient };
    foreach(var sessionID in quizSession.Participants.Keys) {
      var participantClient = clientMgr.GetClient(sessionID);
      if (participantClient != null) clients.Add(participantClient);
    }

    clients.ForEach(client => {
      client.Send(new PKS_CLOSE_QUIZ_SESSION_OK { QuizSessionID = quizSession.QuizSessionID });
      client.ActionState = State.AS_NONE;
      client.QuizSessionID = null;
      client.IsHosted = false;
      client.Questions = [];
      client.Score = 0;
    });
  }

  private readonly ClientMgr clientMgr = _clientMgr;
  private readonly RedisCollection<QuizSession> quizSessionDataSet = (RedisCollection<QuizSession>)_provider.RedisCollection<QuizSession>();
  private readonly DbSet<QuizQuestion> quizQuestionDbSet = _dbContext.Set<QuizQuestion>();
}