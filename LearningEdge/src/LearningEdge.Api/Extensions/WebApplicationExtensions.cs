using LearningEdge.Api.Constants;
using LearningEdge.Api.Hubs;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;

namespace LearningEdge.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION") == "true") 
        {
            return;
        }
        
        const int maxAttempts = 15;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
                app.Logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                app.Logger.LogWarning(
                    ex,
                    "Database is not ready (attempt {Attempt}/{MaxAttempts}). Retrying in 2 seconds...",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException("Database is not reachable.");
    }

    public static WebApplication ConfigureMiddlewarePipelines(this WebApplication app, ConfigurationManager configuration)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("LearningEdge.Api.ExceptionHandler");
                logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred."
                };
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    problem,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            });
        });

        app.UseSerilogRequestLogging()
            .UseHttpsRedirection()
            // .UseRouting() this is not required in .NET 8/9/10 Web APIs, it is managed by the framework automatically
            .UseCors(configuration["Cors:Policy"] ?? DefaultConfigs.CorsPolicyName)
            .UseAuthentication() // will implement authentication in the future
            .UseAuthorization()  // will configure authorization in the future
            .UseRateLimiter();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("LearningEdge API");
                options.AddDocument("v1");
                options.AddDocument("v2");
            }).AllowAnonymous();
        }

        app.UseOutputCache();

        app.MapHub<FileExtractProgressHub>("/hubs/file-extract");

        app.MapHealthChecks("/health");

        return app;
    }
}
