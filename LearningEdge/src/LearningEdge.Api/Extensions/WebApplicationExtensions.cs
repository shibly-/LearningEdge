using Serilog;

namespace LearningEdge.Api.Extensions;

public static class WebApplicationExtensions   
{
    public static WebApplication UseApiDocumentation(this WebApplication app)
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
        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();

        return app;
    }

}
