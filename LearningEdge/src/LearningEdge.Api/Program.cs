using LearningEdge.Api.Extensions;
using LearningEdge.Infrastructure.Persistence;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting application...");

    // Create the application builder
    var builder = WebApplication.CreateBuilder(args);

    // Register services to DI containers
    builder.Services.AddMediatrMapperFluentValidation()
        .AddApplicationDbContext(builder.Configuration)
        .AddOpenApiAndApiVersioning()
        .AddLoggingService(builder.Configuration)
        .AddRateLimiterService()
        .AddControllers();
               

    // Build the application
    var app = builder.Build();

    // Enable middlewares and capabilities  
    app.UseAppDbContextWithDataSeeding()
        .UseOpenApiWithVersioning()
        .UseCustomMiddlewarePipeline()
        .MapControllers();
        
    // Run the application
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly!");
}
finally
{
    Log.Information("Exit application...");
    Log.CloseAndFlush();
}
