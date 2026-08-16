using LearningEdge.Api.Extensions;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Register services to DI containers
    builder.Services.AddMediatrMapperFluentValidation();
    builder.Services.AddApplicationDbContext(builder.Configuration);
    builder.Services.AddOpenApiAndApiVersioning();   
    builder.Services.AddLoggingService(builder.Configuration);    
    builder.Services.AddControllers();

    var app = builder.Build();

    // Enable middlewares and capabilities  
    app.UseAppDbContextWithDataSeeding();
    app.UseApiDocumentation();
    app.UseCustomMiddlewarePipeline();

    app.MapControllers();

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
