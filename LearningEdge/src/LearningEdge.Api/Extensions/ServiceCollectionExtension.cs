using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using LearningEdge.Application;
using LearningEdge.Application.Common.Behaviors;
using LearningEdge.Infrastructure;
using LearningEdge.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using LearningEdge.Messaging.Contracts.Events;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.Hosting;
using LearningEdge.Infrastructure.Messaging.Consumers;

namespace LearningEdge.Api.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddCORSService(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularApp", policy =>
            {
                policy.WithOrigins(configuration["AngularApp:URL"] ?? "http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
        return services;
    }

    public static IServiceCollection AddRateLimiterService(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5; // Max 5 requests
                limiterOptions.Window = TimeSpan.FromSeconds(10); // Per 10 seconds
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2; // Allow 2 queued requests
            });
        });
        return services;
    }         

    public static IServiceCollection AddMediatrMapperFluentValidation(this IServiceCollection services) 
    {
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

    public static IServiceCollection AddApplicationDbContext(
        this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment)
    {
        // Not stored in appsettings: comes from user secrets locally and from the environment in Docker.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set it with " +
                "'dotnet user-secrets set ConnectionStrings:DefaultConnection <value>' or the " +
                "ConnectionStrings__DefaultConnection environment variable.");

        // Register DbContext (Infrastructure)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString, 
                opts => { opts.MigrationsAssembly("LearningEdge.Infrastructure.Migrations"); }
            )
        );

        // Relative paths are resolved against the content root.
        var fileStorageRoot = Path.Combine(
            environment.ContentRootPath,
            configuration["FileStorage:RootPath"] ?? "uploads");

        // Register IApplicationDbContext and IFileStorage for DI
        services.AddInfrastructure(fileStorageRoot);

        return services;
    }
    public static IServiceCollection AddOpenApiAndApiVersioning(this IServiceCollection services)
    {
        services.AddOpenApi("v1");
        services.AddOpenApi("v2");

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
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

    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, ConfigurationManager configuration)
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

                cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", h =>                                    
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ:Password"] ?? "guest");
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

        return services;
    }
}
