using LearningEdge.Api.Extensions;
using LearningEdge.Api.Hubs;
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Caching;
using Serilog;
using StackExchange.Redis;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting LearningEdge Web API...");

    // Create the application builder
    var builder = WebApplication.CreateBuilder(args);

    // Register services to DI containers
    builder.Services.AddCORSService(builder.Configuration)
        .AddMediatrMapperFluentValidation()
        .AddApplicationDbContext(builder.Configuration, builder.Environment)
        .AddOpenApiAndApiVersioning()
        .AddLoggingService(builder.Configuration)
        .AddRateLimiterService()
        .AddMassTransitWithRabbitMQ(builder.Configuration)
        .AddControllers();

    builder.Services.AddSignalR();

    var redisConnection = builder.Configuration["Redis:Connection"] ?? "127.0.0.1:6380";
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    builder.Services.AddSingleton<FileExtractWatchRegistry>();
    builder.Services.AddSingleton<FileExtractProgressReader>();
    builder.Services.AddHostedService<FileExtractProgressPublisher>();

    // Build the application
    var app = builder.Build();

    await app.MigrateDatabaseAsync();

    app.UseOpenApiWithVersioning()
        .UseCustomMiddlewarePipeline()
        .MapControllers();

    app.MapHub<FileExtractProgressHub>("/hubs/file-extract");
        
    // Run the application
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "LearningEdge Web API terminated unexpectedly!");
    throw;
}
finally
{
    Log.Information("Exit LearningEdge Web API...");
    Log.CloseAndFlush();
}
