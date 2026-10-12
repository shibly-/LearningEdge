using LearningEdge.Infrastructure;
using LearningEdge.Infrastructure.Persistence;
using LearningEdgeMS.DocumentParserService.EventConsumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
//using Microsoft.KernelMemory;
//using Microsoft.KernelMemory.DocumentStorage.DevTools;
//using Microsoft.KernelMemory.FileSystem.DevTools;


try
{
    Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

    Log.Information("Starting DocumentParser service...");

    var builder = Host.CreateApplicationBuilder(args);

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set it with " +
            "'dotnet user-secrets set ConnectionStrings:DefaultConnection <value>' or the " +
            "ConnectionStrings__DefaultConnection environment variable.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly("LearningEdge.Infrastructure.Migrations")));

    var storageRoot = ResolvePath(
        builder.Environment.ContentRootPath,
        builder.Configuration["FileStorage:RootPath"] ?? "uploads");
    var documentStorageDirectory = ResolvePath(
        builder.Environment.ContentRootPath,
        builder.Configuration["KernelMemory:DocumentStorageDirectory"] ?? "km-documents");
    var memoryIndex = builder.Configuration["KernelMemory:Index"] ?? "trainings";

    builder.Services.AddSingleton(new DocumentParserSettings(storageRoot, memoryIndex));
    builder.Services.AddInfrastructure(storageRoot);

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

    /*
    builder.Services.AddKernelMemory(memory =>
    {
        var openAi = builder.Configuration.GetSection("KernelMemory:OpenAI").Get<OpenAIConfig>() ?? new OpenAIConfig();
        if (string.IsNullOrWhiteSpace(openAi.APIKey))
        {
            openAi.APIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(openAi.TextModel))
        {
            openAi.TextModel = "gpt-4o-mini";
        }

        if (string.IsNullOrWhiteSpace(openAi.EmbeddingModel))
        {
            openAi.EmbeddingModel = "text-embedding-3-small";
        }

        if (string.IsNullOrWhiteSpace(openAi.APIKey))
        {
            throw new InvalidOperationException(
                "Set KernelMemory:OpenAI:APIKey or the OPENAI_API_KEY environment variable so parsed documents can be embedded into Qdrant.");
        }

        var qdrantEndpoint = builder.Configuration["KernelMemory:Qdrant:Endpoint"] ?? "http ://localhost:6333";
        var qdrantApiKey = builder.Configuration["KernelMemory:Qdrant:APIKey"] ?? string.Empty;

        memory
            .WithOpenAI(openAi)
            .WithQdrantMemoryDb(qdrantEndpoint, qdrantApiKey)
            .WithSimpleFileStorage(new SimpleFileStorageConfig
            {
                StorageType = FileSystemTypes.Disk,
                Directory = documentStorageDirectory
            });
    });
    */

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
