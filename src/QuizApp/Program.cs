using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizApp.Common;
using QuizApp.Core;
using Redis.OM;

var builder = WebApplication.CreateBuilder(args);

var connStringBuilder = new NpgsqlConnectionStringBuilder {
  SslMode = SslMode.VerifyFull,
  Host = builder.Configuration["POSTGRES_HOST"],
  Port = builder.Configuration["POSTGRES_PORT"].ParseInt(),
  Database = builder.Configuration["POSTGRES_DBNAME"],
  Username = builder.Configuration["POSTGRES_USERNAME"],
  Password = builder.Configuration["POSTGRES_PASSWORD"]
};

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStringBuilder.ConnectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

var redisConnectionString = $"{builder.Configuration["REDIS_HOST"]}:{builder.Configuration["REDIS_PORT"]},abortConnect=false";

// Add services to the container.
builder.Services
    .AddDbContext<QuizDbContext>(options => {
      options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention();
    })
    .AddSingleton(new RedisConnectionProvider(redisConnectionString))
    .AddSingleton<ClientMgr>()
    .AddHostedService<ServerBackgroundService>()
    .AddSignalR();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

app.MapHub<ClientHub>("/hub");

app.MapGet("/", () => "Hello World!");

app.Run();
