using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Users;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class UserCommandHandlersTests
{
    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        context.Users.Add(new User("John", "Doe", "john@example.com", UserRole.Learner, organization.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new CreateUserCommand("John", "Doe", "john@example.com", UserRole.Learner, organization.Id),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrganizationDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new UserCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateUserCommand("Ada", "Lovelace", "ada@example.com", UserRole.Instructor, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Handle_ShouldPersistUser_WhenOrganizationExists()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new CreateUserCommand("Ada", "Lovelace", "ada@example.com", UserRole.Instructor, organization.Id),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Handle_ShouldUpdateUser_WhenEmailIsAvailable()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var user = new User("Ada", "Lovelace", "ada@example.com", UserRole.Learner, organization.Id);
        context.Organizations.Add(organization);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "Augusta", "King", "augusta@example.com", UserRole.Instructor, organization.Id),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Augusta", result.Data!.FirstName);
        Assert.Equal("augusta@example.com", result.Data.Email);
        Assert.Equal(UserRole.Instructor, result.Data.Role);

        var saved = await context.Users.SingleAsync();
        Assert.Equal("King", saved.LastName);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var handler = new UserCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", UserRole.Learner, organization.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenEmailBelongsToAnotherUserInTheOrganization()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var current = new User("Ada", "Lovelace", "ada@example.com", UserRole.Learner, organization.Id);
        var other = new User("Grace", "Hopper", "grace@example.com", UserRole.Instructor, organization.Id);
        context.Organizations.Add(organization);
        context.Users.AddRange(current, other);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new UpdateUserCommand(current.Id, "Ada", "Lovelace", "grace@example.com", UserRole.Learner, organization.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
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
