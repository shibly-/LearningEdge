using LearningEdge.Api.Extensions;
using LearningEdge.Api.Hubs;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    Log.Information("Starting LearningEdge Web API...");

    // Create the application builder
    var builder = WebApplication.CreateBuilder(args);

    // Register all required services to DI containers
    // API Services   
    builder.Services.AddApiServices();

    // Application Services
    builder.Services.AddApplicationServices();

    // Infrastructure Services (Data + Other 3rd Party Services)
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    // Messaging / Real-Time Services
    builder.Services.AddMessagingServices(builder.Configuration);

    // Caching Services
    builder.Services.AddCachingServices(builder.Configuration);

    // Background Services
    builder.Services.AddBackgroundServices(builder.Configuration);

    // Cross-Cutting Services
    builder.Services.AddCORSService(builder.Configuration);
    builder.Services.AddRateLimiterService(builder.Configuration);
    builder.Services.AddLoggingService(builder.Configuration);

   
    // Build the application
    var app = builder.Build();

    await app.MigrateDatabaseAsync();

    // Configure the middleware pipeline
    app.ConfigureMiddlewarePipelines(builder.Configuration)         
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
