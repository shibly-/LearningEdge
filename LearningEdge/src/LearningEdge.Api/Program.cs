using LearningEdge.Api.Extensions;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting LearningEdge Web API...");

    // Create the application builder
    var builder = WebApplication.CreateBuilder(args);

    // Register services to DI containers
    builder.Services.AddCORSService()
        .AddMediatrMapperFluentValidation()
        .AddApplicationDbContext(builder.Configuration, builder.Environment)
        .AddOpenApiAndApiVersioning()
        .AddLoggingService(builder.Configuration)
        .AddRateLimiterService()
        .AddMassTransitWithRabbitMQ(builder.Configuration)
        .AddControllers();

    // Build the application
    var app = builder.Build();

    await app.MigrateDatabaseAsync();

    app.UseOpenApiWithVersioning()
        .UseCustomMiddlewarePipeline()
        .MapControllers();
        
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
