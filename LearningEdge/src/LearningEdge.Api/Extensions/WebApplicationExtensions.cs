using LearningEdge.Infrastructure.Persistence;
using Serilog;

namespace LearningEdge.Api.Extensions;

public static class WebApplicationExtensions   
{

    public static WebApplication UseAppDbContextWithDataSeeding(this WebApplication app)
    {
        // Seed the application database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
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
