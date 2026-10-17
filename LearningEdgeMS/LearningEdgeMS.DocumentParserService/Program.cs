
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Caching;
using LearningEdgeMS.DocumentParserService.Extensions;
using LearningEdgeMS.DocumentParserService.Services;
using Serilog;
using StackExchange.Redis;

try
{
    Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

    Log.Information("Starting LearningEdge DocumentParser service...");

    var builder = Host.CreateApplicationBuilder(args);

    // Register required services and configurations
    builder.Services.AddSerilog()
        .AddApplicationDbContext(builder.Configuration)
        .AddEmbeddingAndVectorDBService(builder.Configuration, builder.Environment)
        .AddMassTransitWithRabbitMQ(builder.Configuration, builder.Environment);

    var redisConnection = builder.Configuration["Redis:Connection"] ?? "127.0.0.1:6380";
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    // Register custom services & default background worker
    builder.Services
        .AddSingleton<DocumentParsingService>()
        .AddSingleton<EmbeddingService>()
        .AddHostedService<DefaultWorkerService>();    

    var host = builder.Build();
    host.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "LearningEdge DocumentParser service terminated unexpectedly!");
    throw;
}
finally
{
    Log.Information("Exit LearningEdge DocumentParser service...");
    Log.CloseAndFlush();
}


