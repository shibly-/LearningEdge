using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using LearningEdge.Infrastructure;
using LearningEdge.Infrastructure.Persistence;
using LearningEdgeMS.DocumentParserService.EventConsumers;
using LearningEdgeMS.DocumentParserService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OllamaSharp;
using Qdrant.Client;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

    Log.Information("Starting DocumentParser service...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set it with " +
            "'dotnet user-secrets set ConnectionStrings:DefaultConnection <value>' or the " +
            "ConnectionStrings__DefaultConnection environment variable.");

    // Register EF Core DBContext with SQL Server provider
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly("LearningEdge.Infrastructure.Migrations")));

    var storageRoot = ResolvePath(
        builder.Environment.ContentRootPath,
        builder.Configuration["FileStorage:RootPath"] ?? "uploads");
    
    builder.Services.AddSingleton(new DocumentParserSettings(storageRoot));

    // Register Ollama Embedding Generator via Microsoft.Extensions.AI
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        new OllamaApiClient(new Uri("http://localhost:11434"), "qwen3-embedding:0.6b"));
    
    // Register Qdrant Client (gRPC)
    builder.Services.AddSingleton<QdrantClient>(sp =>
        new QdrantClient(host: "localhost", port: 6334));
    
    // Register MassTransit with RabbitMQ
    builder.Services.AddMassTransit(bus =>
    {
        bus.AddConsumer<InitiateFileExtractConsumer>();

        bus.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(
                builder.Configuration["RabbitMQ:Host"] ?? "localhost",
                builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
                host =>
                {
                    host.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                    host.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
                });

            cfg.ReceiveEndpoint("file-extract-queue", endpoint =>
            {
                endpoint.ConfigureConsumer<InitiateFileExtractConsumer>(context);
            });
        });
    });

    // Register Custom Services
    builder.Services.AddSingleton<DocumentParsingService>();
    builder.Services.AddSingleton<EmbeddingService>();

    // Register Background Worker
    builder.Services.AddHostedService<DefaultWorkerService>();    

    var host = builder.Build();
    host.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "DocumentParser service terminated unexpectedly!");
    throw;
}
finally
{
    Log.Information("Exit DocumentParser service...");
    Log.CloseAndFlush();
}

static string ResolvePath(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(contentRoot, path));
