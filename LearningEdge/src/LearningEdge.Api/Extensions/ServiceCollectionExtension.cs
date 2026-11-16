using Asp.Versioning;
using Azure;
using FluentValidation;
using LearningEdge.Api.Constants;
using LearningEdge.Api.Hubs;
using LearningEdge.Application;
using LearningEdge.Application.Common.Behaviors;
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure;
using LearningEdge.Api.Security;
using LearningEdge.Infrastructure.Caching;
using LearningEdge.Infrastructure.Identity;
using LearningEdge.Infrastructure.Messaging.Consumers;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace LearningEdge.Api.Extensions;

public static class ServiceCollectionExtension
{
    private static readonly string AutoMapperLicense = Environment.GetEnvironmentVariable("AUTOMAPPER_LICENSE_PATH") ?? string.Empty;
    
    public static IServiceCollection AddApiServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = configuration["Keycloak:Authority"];
            options.RequireHttpsMetadata = bool.Parse(configuration["Keycloak:RequireHttpsMetadata"] ?? "false");
            options.TokenValidationParameters.ValidAudience = configuration["Keycloak:APIClientId"];
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            configuration["Keycloak:ConfigurationMetadataURL"],
            new OpenIdConnectConfigurationRetriever());
            options.RefreshInterval = TimeSpan.FromMinutes(int.Parse(configuration["Keycloak:RefreshInterval"] ?? "5"));
            options.AutomaticRefreshInterval = TimeSpan.FromHours(int.Parse(configuration["Keycloak:AutomaticRefreshInterval"] ?? "12"));
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        KeycloakRoles.EnsureRoleClaims(identity);
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.SysAdmin, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => KeycloakRoles.Has(context.User, AuthPolicies.SysAdmin)));

        // Add Controllers
        services.AddControllers();

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;            
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
        services.AddOpenApi(configuration["OpenApi:v1:Version"] ?? "v1", options =>
        {
            options.AddDocumentTransformer((document, ctx, ct) =>
            {
                document.Info.Title = configuration["OpenApi:v1:Title"] ?? "LearningEdge API";
                document.Info.Version = configuration["OpenApi:v1:Version"] ?? "v1";

                document.Components ??= new();
                document.Components.SecuritySchemes ??=
    new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes.Add(
                    configuration["Keycloak:Provider"] ?? string.Empty,
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(configuration["Keycloak:AuthorizationUrl"] ?? string.Empty),
                                TokenUrl = new Uri(configuration["Keycloak:TokenUrl"] ?? string.Empty),
                                Scopes = new Dictionary<string, string>
                                {
                                    ["openid"] = configuration["Keycloak:ScopesOpenid"] ?? string.Empty,
                                    ["profile"] = configuration["Keycloak:ScopesProfile"] ?? string.Empty
                                }
                            }
                        }
                    });

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var requiresAuth =
                    context.Description.ActionDescriptor.EndpointMetadata
                        .OfType<AuthorizeAttribute>()
                        .Any();

                if (!requiresAuth)
                {
                    return Task.CompletedTask;
                }

                operation.Security ??= [];

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(configuration["Keycloak:Provider"] ?? string.Empty)
                        ] = []
                    });

                return Task.CompletedTask;
            });

        });

        services.AddOpenApi(configuration["OpenApi:v2:Version"] ?? "v2", options =>
        {
            options.AddDocumentTransformer((document, ctx, ct) =>
            {
                document.Info.Title = configuration["OpenApi:v2:Title"] ?? "LearningEdge API - Extended";
                document.Info.Version = configuration["OpenApi:v2:Version"] ?? "v2";
                return Task.CompletedTask;
            });
        });

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
        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = AutoMapperLicense;
            cfg.AddMaps(typeof(AssemblyMarker).Assembly);
        });

        // FluentValidation → scans Application assembly for validators
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, 
        ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(DefaultConfigs.DefaultConnectionExceptionMessage);
        var migrationAssembly = configuration["MigrationsAssembly"] ?? DefaultConfigs.MigrationsAssembly;
        var fileStorageRoot = Path.Combine(environment.ContentRootPath,configuration["FileStorage:RootPath"] ?? DefaultConfigs.FileStorageRootPath);

        services.AddInfrastructure(connectionString, migrationAssembly, fileStorageRoot);

        var keysPath = Path.Combine(environment.ContentRootPath, "dp-keys");
        Directory.CreateDirectory(keysPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        services.AddSingleton<IPendingCredentialProtector, PendingCredentialProtector>();
        services.AddSingleton(BuildKeycloakAdminSettings(configuration));
        services.AddHttpClient<IKeycloakUserDirectory, KeycloakAdminClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    private static KeycloakAdminSettings BuildKeycloakAdminSettings(IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"];
        if (string.IsNullOrWhiteSpace(authority) || !Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
        {
            throw new InvalidOperationException("Keycloak:Authority must be an absolute URL.");
        }

        var segments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var realm = segments.Length > 0 ? segments[^1] : string.Empty;
        if (string.IsNullOrWhiteSpace(realm))
        {
            throw new InvalidOperationException("Keycloak:Authority must include the realm.");
        }

        var origin = authorityUri.GetLeftPart(UriPartial.Authority);
        var learnerRole = configuration["Keycloak:LearnerRole"];
        var orgAdminRole = configuration["Keycloak:OrgAdminRole"];
        return new KeycloakAdminSettings
        {
            TokenUrl = configuration["Keycloak:TokenUrl"]
                ?? $"{origin}/realms/{realm}/protocol/openid-connect/token",
            AdminRoot = $"{origin}/admin/realms/{realm}",
            ClientId = configuration["Keycloak:AdminClientId"] ?? string.Empty,
            ClientSecret = configuration["Keycloak:AdminClientSecret"] ?? string.Empty,
            LearnerRole = string.IsNullOrWhiteSpace(learnerRole) ? "learner" : learnerRole,
            OrgAdminRole = string.IsNullOrWhiteSpace(orgAdminRole) ? "OrgAdmin" : orgAdminRole,
        };
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

                cfg.Host(configuration["RabbitMQ:Host"] ?? DefaultConfigs.RabbitMQDefaultHost, h =>                                    
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? DefaultConfigs.RabbitMQDefaultUser);
                    h.Password(configuration["RabbitMQ:Password"] ?? DefaultConfigs.RabbitMQDefaultPassword);
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
        var redisConnection = configuration["Redis:Connection"] ?? DefaultConfigs.RedisConnectionString;
        
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection))
            .AddSingleton<ICacheService, RedisCacheService>();

        // add

        services.AddOutputCache(options =>
        {
            // Define a custom named policy for read endpoints
            options.AddPolicy(CachePolicies.DefaultQueryCache, policy =>
            {
                policy.Expire(TimeSpan.FromSeconds(configuration.GetValue<int>("OutputCache:DefaultQueryCache:ExpiryPeriodInSeconds")));
            });
        });

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
            options.AddPolicy(configuration["Cors:Policy"] ?? DefaultConfigs.CorsPolicyName, policy =>
            {
                policy.WithOrigins(configuration["Cors:Origin"] ?? DefaultConfigs.CorsOrigin)
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
            options.AddTokenBucketLimiter(configuration["RateLimiter:TokenBucket:PolicyName"] ?? DefaultConfigs.RateLimiterTokenBucketPolicyName, opt =>
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
