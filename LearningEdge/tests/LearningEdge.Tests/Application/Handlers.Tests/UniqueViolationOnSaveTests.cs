using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Organizations;
using LearningEdge.Application.Handlers.Users;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Application.Handlers.Tests;

// Simulates a concurrent request winning the race: the handler's pre-check passes,
// but the database rejects the save with a unique-index violation.
public class UniqueViolationOnSaveTests
{
    [Fact]
    public async Task CreateOrganization_ReturnsConflict_WhenSaveHitsUniqueIndex()
    {
        await using var context = CreateContext();
        var handler = new OrganizationCommandHandlers(new FailingSaveContext(context), CreateMapper());

        var result = await handler.Handle(new CreateOrganizationCommand("Acme", "A school"), CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task UpdateOrganization_ReturnsConflict_WhenSaveHitsUniqueIndex()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var handler = new OrganizationCommandHandlers(new FailingSaveContext(context), CreateMapper());
        var result = await handler.Handle(
            new UpdateOrganizationCommand(organization.Id, "Contoso", "Renamed"), CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenSaveHitsUniqueIndex()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var handler = new UserCommandHandlers(new FailingSaveContext(context), CreateMapper());
        var result = await handler.Handle(
            new CreateUserCommand("Ada", "Lovelace", "ada@acme.test", UserRole.OrgAdmin, organization.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task UpdateUser_ReturnsConflict_WhenSaveHitsUniqueIndex()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var user = new User("Ada", "Lovelace", "ada@acme.test", UserRole.OrgAdmin, organization.Id);
        context.Organizations.Add(organization);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UserCommandHandlers(new FailingSaveContext(context), CreateMapper());
        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "Ada", "Lovelace", "ada.new@acme.test", UserRole.OrgAdmin, organization.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    private sealed class FailingSaveContext(ApplicationDbContext inner) : IApplicationDbContext
    {
        public DbSet<Organization> Organizations => inner.Organizations;
        public DbSet<User> Users => inner.Users;
        public DbSet<Category> Categories => inner.Categories;
        public DbSet<Training> Trainings => inner.Trainings;
        public DbSet<TrainingFile> TrainingFiles => inner.TrainingFiles;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new UniqueConstraintViolationException("Cannot insert duplicate key row.", new Exception());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile<OrganizationProfile>();
            config.AddProfile<UserProfile>();
        });
        return configuration.CreateMapper();
    }
}
