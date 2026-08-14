using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Serilog;

namespace LearningEdge.Api.Extensions;

public static class ServiceCollectionExtension
{
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
