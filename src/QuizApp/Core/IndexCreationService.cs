
using Redis.OM;

namespace QuizApp.Core;

public class IndexCreationService(RedisConnectionProvider _provider) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    await provider.Connection.CreateIndexAsync(typeof(QuizSession));
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }

  private readonly RedisConnectionProvider provider = _provider;
}