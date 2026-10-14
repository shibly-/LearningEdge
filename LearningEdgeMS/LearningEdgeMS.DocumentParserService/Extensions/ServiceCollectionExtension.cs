using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Qdrant.Client;
using LearningEdgeMS.DocumentParserService.Messaging.Consumers;

namespace LearningEdgeMS.DocumentParserService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static string GetCollectionName(IConfiguration configuration)
        {
            return configuration["VectorDB:Qdrant:CollectionName"] ?? "le_docs";
        }

        // Add ApplicationDbContext with MSSQL Server configuration
        public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, ConfigurationManager configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set it with " +
            "'dotnet user-secrets set ConnectionStrings:DefaultConnection <value>' or the " +
            "ConnectionStrings__DefaultConnection environment variable.");

            // Register EF Core DBContext with SQL Server provider
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly("LearningEdge.Infrastructure.Migrations")));

            return services;
        }

        // Add Embedding and Vector Database Services
        public static IServiceCollection AddEmbeddingAndVectorDBService(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var storageRoot = ResolvePath(environment.ContentRootPath, configuration["FileStorage:RootPath"] ?? "uploads");

            services.AddSingleton(new DocumentParserSettings(storageRoot));

            // Register Ollama Embedding Generator via Microsoft.Extensions.AI
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
                new OllamaApiClient(new Uri("http://localhost:11434"), "qwen3-embedding:0.6b"));

            // Register Qdrant Client (gRPC)
            services.AddSingleton<QdrantClient>(sp =>
                new QdrantClient(host: "localhost", port: 6334));

            return services;
        }

        // Add MassTransit with RabbitMQ
        public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddMassTransit(bus =>
            {
                bus.AddConsumer<FileExtractRequestConsumer>();

                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        configuration["RabbitMQ:Host"] ?? "localhost",
                        configuration["RabbitMQ:VirtualHost"] ?? "/",
                        host =>
                        {
                            host.Username(configuration["RabbitMQ:Username"] ?? "guest");
                            host.Password(configuration["RabbitMQ:Password"] ?? "guest");
                        });

                    cfg.ReceiveEndpoint(configuration["RabbitMQ:FileExtractRequestQueue"] ?? "le-file-extract-requested", endpoint =>
                    {
                        endpoint.ConfigureConsumer<FileExtractRequestConsumer>(context);
                    });
                });
            });

            return services;
        }

        // Helper method to resolve the storage path
        static string ResolvePath(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(contentRoot, path));
    }
}
