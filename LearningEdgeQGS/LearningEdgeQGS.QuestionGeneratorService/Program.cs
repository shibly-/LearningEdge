using LearningEdgeMS.QuestionGeneratorService.Extensions;
using LearningEdgeMS.QuestionGeneratorService.Services;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

    Log.Information("Starting LearningEdge QuestionGenerator service...");

    var builder = Host.CreateApplicationBuilder(args);

    // Register required services and configurations
    builder.Services.AddSerilog().AddEmbeddingAndVectorDBService(builder.Configuration, builder.Environment);
    
    // Register custom services & default background worker
    builder.Services.AddHostedService<DefaultWorkerService>();    

    var host = builder.Build();
    host.Run();
}
catch (Exception ex) when(ex is not HostAbortedException)
{
    Log.Fatal(ex, "LearningEdge QuestionGenerator service terminated unexpectedly!");
    throw;
}
finally
{
    Log.Information("Exit LearningEdge QuestionGenerator service...");
    Log.CloseAndFlush();
}


