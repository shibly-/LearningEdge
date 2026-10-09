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


    // Configure MassTransit with RabbitMQ
    //builder.Services.AddMassTransit(x =>
    //{
    //    // Automatically register all consumers in the executing assembly
    //    x.AddConsumers(typeof(Program).Assembly);

    //    x.UsingRabbitMq((context, cfg) =>
    //    {
    //        cfg.Host("localhost", "/", h =>
    //        {
    //            h.Username("guest");
    //            h.Password("guest");
    //        });
    //    });
    //});

    //builder.Services.AddMassTransitHostedService();


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
