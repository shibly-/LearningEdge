using Asp.Versioning;
using FluentValidation;
using LearningEdge.Api.Hubs;
using LearningEdge.Application;
using LearningEdge.Application.Common.Behaviors;
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure;
using LearningEdge.Infrastructure.Caching;
using LearningEdge.Infrastructure.Messaging.Consumers;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace LearningEdge.Api.Extensions;

public static class ServiceCollectionExtension
{
    private const string CorsPolicyName = "AllowWebApp";
    private const string CorsOrigin = "http://localhost:4200";
    private const string RateLimiterTokenBucketPolicyName = "api";
    private const string FileStorageRootPath = "uploads";
    private const string RedisConnectionString = "127.0.0.1:6380";
    private const string RabbitMQDefaultHost = "localhost";
    private const string RabbitMQDefaultUser = "guest";   
    private const string RabbitMQDefaultPassword = "guest";

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Add Controllers
        services.AddControllers();

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        //.AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add OpenAPI/Swagger support
        services.AddOpenApi("v1");
        services.AddOpenApi("v2");

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services) 
    {
        // Add MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // AutoMapper → scans Application assembly for profiles
        services.AddAutoMapper(typeof(AssemblyMarker).Assembly);

        // FluentValidation → scans Application assembly for validators
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment)
    {
        // Not stored in appsettings: comes from user secrets locally and from the environment in Docker.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        // Register App DbContext (Infrastructure)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString, 
                opts => { opts.MigrationsAssembly(configuration["MigrationsAssembly"] ?? "LearningEdge.Infrastructure.Migrations"); }
            )
        );

        // Register Other Infrastructure Services
        // Relative paths are resolved against the content root.
        var fileStorageRoot = Path.Combine(
            environment.ContentRootPath,
            configuration["FileStorage:RootPath"] ?? FileStorageRootPath);

        services.AddInfrastructure(fileStorageRoot);

        return services;
    }    

    public static IServiceCollection AddMessagingServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        // Register MassTransit
        services.AddMassTransit(bus =>
        {
            // Register consumers
            bus.AddConsumer<ExtractRequestCompletedConsumer>();

            // Configure the transport with RabbitMQ (In future, we will configure for Azure Service Bus)
            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Message<IExtractRequestSubmitted>(message =>
                    message.SetEntityName(configuration["RabbitMQ:FileExtractRequestEvent"] ?? "le-file-extract-requested"));

                //cfg.Message<IQuestionGenerationSubmitted>(message =>
                //    message.SetEntityName(configuration["RabbitMQ:QuestionGenerationRequestEvent"] ?? "le-question-generation-requested"));

                cfg.Host(configuration["RabbitMQ:Host"] ?? RabbitMQDefaultHost, h =>                                    
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? RabbitMQDefaultUser);
                    h.Password(configuration["RabbitMQ:Password"] ?? RabbitMQDefaultPassword);
                });

                // Automatically configure endpoints for all registered consumers
                // cfg.ConfigureEndpoints(context);

                // configure endpoint for FileExtractCompleted consumers
                cfg.ReceiveEndpoint(configuration["RabbitMQ:FileExtractCompletedQueue"] ?? string.Empty, endpoint =>
                {
                    endpoint.ConfigureConsumer<ExtractRequestCompletedConsumer>(context);
                });

                // configure endpoint for QuestionGenerationCompleted consumers
                //cfg.ReceiveEndpoint(configuration["RabbitMQ:QuestionGenerationCompletedQueue"] ?? string.Empty, endpoint =>
                //{
                //    endpoint.ConfigureConsumer<QuestionGenerationCompletedConsumer>(context);
                //});

                //cfg.Durable = true;
                //cfg.AutoDelete = false;            
                //cfg.Exclusive = false;
            });
        });

        services.AddSignalR();

        return services;
    }

    public static IServiceCollection AddCachingServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        var redisConnection = configuration["Redis:Connection"] ?? RedisConnectionString;
        
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection))
            .AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSingleton<FileExtractWatchRegistry>()
            .AddSingleton<FileExtractProgressReader>()
            .AddHostedService<FileExtractProgressPublisher>();

        return services;
    }

    public static IServiceCollection AddCORSService(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(configuration["Cors:Policy"] ?? CorsPolicyName, policy =>
            {
                policy.WithOrigins(configuration["Cors:Origin"] ?? CorsOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        return services;
    }

    public static IServiceCollection AddRateLimiterService(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.AddTokenBucketLimiter(configuration["RateLimiter:TokenBucket:PolicyName"] ?? RateLimiterTokenBucketPolicyName, opt =>
            {
                opt.TokenLimit = configuration.GetValue<int>("RateLimiter:TokenBucket:TokenLimit");
                opt.TokensPerPeriod = configuration.GetValue<int>("RateLimiter:TokenBucket:TokensPerPeriod");
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiter:TokenBucket:ReplenishmentPeriodInSeconds"));
                opt.QueueLimit = configuration.GetValue<int>("RateLimiter:TokenBucket:QueueLimit");
            });
        });
        return services;
    }

    public static IServiceCollection AddLoggingService(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(configuration)
        .ReadFrom.Services(services));

        return services;
    }
}
