
using LearningEdgeMS.DocumentParserService.Extensions;
using LearningEdgeMS.DocumentParserService.Services;
using Serilog;

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


