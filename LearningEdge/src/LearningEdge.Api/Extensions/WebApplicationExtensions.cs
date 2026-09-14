using LearningEdge.Infrastructure.Persistence;
using Serilog;

namespace LearningEdge.Api.Extensions;

public static class WebApplicationExtensions   
{

    public static WebApplication UseAppDbContextWithDataSeeding(this WebApplication app)
    {
        // Microsoft.Extensions.ApiDescription.Server starts the app at build time
        // to generate OpenAPI documents. There is no SQL Server in that process.
        var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        if (string.Equals(entryAssemblyName, "GetDocument.Insider", StringComparison.Ordinal))
        {
            return app;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");

        const int maxAttempts = 15;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                context.Database.EnsureCreated();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Database is not ready (attempt {Attempt}/{MaxAttempts}). Retrying in 2 seconds...",
                    attempt,
                    maxAttempts);
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        //if (app.Environment.IsDevelopment())
        //{
            //await app.Services.InitializeDatabaseAsync();
        //}

        return app;
    }

    public static WebApplication UseOpenApiWithVersioning(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/{documentName}.json");
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "v1");
                options.SwaggerEndpoint("/openapi/v2.json", "v2");
            });
        }

        return app;
    }

    public static WebApplication UseCustomMiddlewarePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging()
            .UseHttpsRedirection()
            .UseAuthorization()
            .UseRateLimiter();

        return app;
    }

}
