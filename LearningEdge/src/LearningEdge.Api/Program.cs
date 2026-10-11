using LearningEdge.Api.Extensions;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting application...");

    // Create the application builder
    var builder = WebApplication.CreateBuilder(args);

    // Add CORS policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularApp", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // Register services to DI containers
    builder.Services.AddMediatrMapperFluentValidation()
        .AddApplicationDbContext(builder.Configuration, builder.Environment)
        .AddOpenApiAndApiVersioning()
        .AddLoggingService(builder.Configuration)
        .AddRateLimiterService()
        .AddControllers();


    // Register MassTransit
    builder.Services.AddMassTransit(x =>
    {
        // 1. Register your consumers here (if you have any)
        // x.AddConsumer<YourMessageConsumer>();

        // 2. Configure the transport (e.g., RabbitMQ)
        x.UsingRabbitMq((context, cfg) =>
        {
            // "rabbitmq" is the service name defined in docker-compose.yml
            //cfg.Host("rabbitmq", "/", h =>
            // when running the API outside of docker
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            // Automatically configure endpoints for all registered consumers
            cfg.ConfigureEndpoints(context);
        });
    });


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
    Log.Fatal(ex, "Application terminated unexpectedly!");
    throw;
}
finally
{
    Log.Information("Exit application...");
    Log.CloseAndFlush();
}
