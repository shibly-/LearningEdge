using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using LearningEdge.Application;
using LearningEdge.Infrastructure;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

namespace LearningEdge.Api.Extensions;

public static class ServiceCollectionExtension
{
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
        // MediatR → scans Application assembly for handlers
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

        // AutoMapper → scans Application assembly for profiles
        services.AddAutoMapper(typeof(AssemblyMarker).Assembly);

        // FluentValidation → scans Application assembly for validators
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services;
    }

    public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, ConfigurationManager configuration)
    {
        // Register DbContext (Infrastructure)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("LearningEdgeDBConnectionDockerDev"), 
                opts => { opts.MigrationsAssembly("LearningEdge.Infrastructure.Migrations"); }
            )
        );

        // Register IApplicationDbContext for DI    
        services.AddInfrastructure();

        return services;
    }
    public static IServiceCollection AddOpenApiAndApiVersioning(this IServiceCollection services)
    {
        // We don't need to customize the API versioning options for this example as we are using query string versioning.
        services.AddOpenApi("v1");
        services.AddOpenApi("v2");

        services.AddApiVersioning()
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
}
