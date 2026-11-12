using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Constants;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearningEdge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string migrationAssembly, string fileStorageRootPath)
    {
        // Register App DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                opts => { 
                    opts.MigrationsAssembly(migrationAssembly); 
                    //opts.EnableRetryOnFailure(DbContextDefaultOptions.MaxRetryCount, TimeSpan.FromSeconds(DbContextDefaultOptions.MaxRetryDelay), null);
                }
            )
        );

        // Map the interface to the concrete type
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register File Storage Service, currently a local file system implementation. Can be replaced with a cloud storage implementation in the future.
        services.AddSingleton<IFileStorage>(new LocalFileStorage(fileStorageRootPath));
        return services;
    }
}
