using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Users;
using LearningEdge.Application.Models.Queries.Users;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class UserQueryHandlersTests
{
    [Fact]
    public async Task Handle_ShouldReturnUser_WhenUserExists()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var user = new User("Ada", "Lovelace", "ada@example.com", UserRole.Instructor, organization.Id);
        context.Organizations.Add(organization);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserQueryHandlers(context, CreateMapper());
        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(user.Id, result.Data!.Id);
        Assert.Equal("Ada", result.Data.FirstName);
        Assert.Equal("Lovelace", result.Data.LastName);
        Assert.Equal("ada@example.com", result.Data.Email);
        Assert.Equal(UserRole.Instructor, result.Data.Role);
        Assert.Equal(organization.Id, result.Data.OrganizationId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new UserQueryHandlers(context, CreateMapper());

        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
        Assert.Contains("No user found", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserIsDeleted()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var user = new User("Ada", "Lovelace", "ada@example.com", UserRole.Learner, organization.Id);
        user.SoftDelete();
        context.Organizations.Add(organization);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserQueryHandlers(context, CreateMapper());
        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetUsersByOrganization_ReturnsOnlyActiveUsersOfThatOrganization_Ordered()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var other = new Organization("Contoso");
        var deleted = new User("Dee", "Leted", "dee@acme.test", UserRole.Learner, organization.Id);
        deleted.SoftDelete();
        context.Organizations.AddRange(organization, other);
        context.Users.AddRange(
            new User("Grace", "Hopper", "grace@acme.test", UserRole.Instructor, organization.Id),
            new User("Ada", "Lovelace", "ada@acme.test", UserRole.OrgAdmin, organization.Id),
            new User("Alan", "Hopper", "alan@acme.test", UserRole.Learner, organization.Id),
            new User("Linus", "Torvalds", "linus@contoso.test", UserRole.Learner, other.Id),
            deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserQueryHandlers(context, CreateMapper());
        var result = await handler.Handle(new GetUsersByOrganizationQuery(organization.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(
            ["alan@acme.test", "grace@acme.test", "ada@acme.test"],
            result.Data!.Select(user => user.Email));
    }

    [Fact]
    public async Task GetUsersByOrganization_ReturnsEmptyList_WhenOrganizationHasNoUsers()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var handler = new UserQueryHandlers(context, CreateMapper());
        var result = await handler.Handle(new GetUsersByOrganizationQuery(organization.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetUsersByOrganization_ReturnsNotFound_WhenOrganizationDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new UserQueryHandlers(context, CreateMapper());

        var result = await handler.Handle(new GetUsersByOrganizationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
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
        var configuration = new MapperConfiguration(config => config.AddProfile<UserProfile>());
        return configuration.CreateMapper();
    }
}
