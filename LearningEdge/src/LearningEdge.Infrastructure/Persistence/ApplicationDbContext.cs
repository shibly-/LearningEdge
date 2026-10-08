using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Interfaces;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    // SQL Server error numbers for duplicate keys in a unique index and a unique constraint.
    private static readonly int[] UniqueViolationErrorNumbers = [2601, 2627];

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingFile> TrainingFiles => Set<TrainingFile>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
            && UniqueViolationErrorNumbers.Contains(sql.Number))
        {
            throw new UniqueConstraintViolationException(sql.Message, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
