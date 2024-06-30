namespace QuizApp.Core;

public class ServerBackgroundService(ClientMgr _clientMgr) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
      clientMgr.Update();
      await Task.Delay(120, stoppingToken);
    }
  }

  private readonly ClientMgr clientMgr = _clientMgr;
}