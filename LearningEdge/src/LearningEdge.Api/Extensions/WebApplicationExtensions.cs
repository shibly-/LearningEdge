using System.Text.Json;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LearningEdge.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        // Microsoft.Extensions.ApiDescription.Server starts the app at build time
        // to generate OpenAPI documents. There is no SQL Server in that process.
        var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        if (string.Equals(entryAssemblyName, "GetDocument.Insider", StringComparison.Ordinal))
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
            .UseCors("AllowAngularApp")
            .UseAuthorization()
            .UseRateLimiter();

        return app;
    }
}
