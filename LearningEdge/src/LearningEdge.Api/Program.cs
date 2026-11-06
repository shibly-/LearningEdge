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
    builder.Services.AddApiServices() // API Services   
        .AddApplicationServices() // Application Services
        .AddInfrastructureServices(builder.Configuration, builder.Environment) // Infrastructure Services 
        .AddMessagingServices(builder.Configuration) // Messaging / Real-Time Services
        .AddCachingServices(builder.Configuration) // Caching Services
        .AddBackgroundServices(builder.Configuration) // Background Services
        .AddCORSService(builder.Configuration) // CORS Services
        .AddRateLimiterService(builder.Configuration) // Rate Limiter Services
        .AddLoggingService(builder.Configuration); // Logging Services

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
