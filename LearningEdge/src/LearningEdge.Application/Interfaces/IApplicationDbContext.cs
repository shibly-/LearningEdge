using Microsoft.EntityFrameworkCore;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Users;

namespace LearningEdge.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }      
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
