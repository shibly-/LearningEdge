using AutoMapper;
using FluentValidation;
using LearningEdge.Api.Extensions;
using LearningEdge.Application;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Register DbContext (Infrastructure)
    builder.Services.AddDbContext<ApplicationDbContext>(options => 
        options.UseSqlServer(builder.Configuration.GetConnectionString("LearningEdgeDBConnection")));

    // Register interface for DI
    builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>()!);
    
    // MediatR → scans Application assembly for handlers
    builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

    // AutoMapper → scans Application assembly for profiles
    builder.Services.AddAutoMapper(typeof(AssemblyMarker).Assembly);

    // FluentValidation → scans Application assembly for validators
    builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

    // Add services to the container.
    builder.Services.AddControllers();

    // Register services from ServiceCollectionExtension.cs
    builder.Services.AddOpenApiAndApiVersioning();   
    builder.Services.AddLoggingService(builder.Configuration);

    var app = builder.Build();

    // invoked from WebApplicationExtensions.cs
    app.UseApiDocumentation();
    app.UseCustomMiddlewarePipeline();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly!");
}
finally
{
    Log.Information("Exit application...");
    Log.CloseAndFlush();
}
