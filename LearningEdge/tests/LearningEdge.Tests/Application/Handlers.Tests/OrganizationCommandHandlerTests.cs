using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Organizations;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class OrganizationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrganization_WhenNameIsAvailable()
    {
        await using var context = CreateContext();
        var handler = new OrganizationCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateOrganizationCommand("Acme", "A school"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);

        var saved = await context.Organizations.SingleAsync();
        Assert.Equal(result.Data, saved.Id);
        Assert.Equal("Acme", saved.Name);
        Assert.Equal("A school", saved.Description);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenOrganizationNameAlreadyExists()
    {
        await using var context = CreateContext();
        var existing = new Organization("Acme");
        context.Organizations.Add(existing);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new OrganizationCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new CreateOrganizationCommand("Acme", "Duplicate"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
        Assert.Contains(existing.Id.ToString(), result.Error);
        Assert.Equal(1, await context.Organizations.CountAsync());
    }

    [Fact]
    public async Task Handle_ShouldStoreEmptyDescription_WhenDescriptionIsNull()
    {
        await using var context = CreateContext();
        var handler = new OrganizationCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateOrganizationCommand("Acme", null!),
            CancellationToken.None);

        Assert.True(result.Success);
        var saved = await context.Organizations.SingleAsync();
        Assert.Equal(string.Empty, saved.Description);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrganization_WhenOnlyADeletedOrganizationHasTheName()
    {
        await using var context = CreateContext();
        var deleted = new Organization("Acme");
        deleted.SoftDelete();
        context.Organizations.Add(deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new OrganizationCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new CreateOrganizationCommand("Acme", "Replacement"),
            CancellationToken.None);

        Assert.True(result.Success);
        var visible = await context.Organizations.SingleAsync();
        Assert.Equal(result.Data, visible.Id);
        Assert.Equal("Replacement", visible.Description);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOrganization_WhenNameIsAvailable()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme", "Before");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new OrganizationCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new UpdateOrganizationCommand(organization.Id, "Acme Updated", "After"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Acme Updated", result.Data!.Name);
        Assert.Equal("After", result.Data.Description);

        var saved = await context.Organizations.SingleAsync();
        Assert.Equal("Acme Updated", saved.Name);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrganizationDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new OrganizationCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new UpdateOrganizationCommand(Guid.NewGuid(), "Missing", "None"),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAnotherOrganizationHasTheName()
    {
        await using var context = CreateContext();
        var current = new Organization("Acme");
        var other = new Organization("Contoso");
        context.Organizations.AddRange(current, other);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new OrganizationCommandHandlers(context, CreateMapper());
        var result = await handler.Handle(
            new UpdateOrganizationCommand(current.Id, "Contoso", "Taken"),
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
        var configuration = new MapperConfiguration(config => config.AddProfile<OrganizationProfile>());
        return configuration.CreateMapper();
    }
}
