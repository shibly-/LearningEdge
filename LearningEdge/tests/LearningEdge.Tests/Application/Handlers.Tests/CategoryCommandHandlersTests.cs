using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Categories;
using LearningEdge.Application.Models.Commands.Categories;
using LearningEdge.Application.Models.Queries.Categories;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class CategoryCommandHandlersTests
{
    [Fact]
    public async Task Create_SavesCategory_WhenOrganizationExists()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationAsync(context);
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateCategoryCommand(organization.Id, "Safety", "Safety courses", IsActive: false),
            CancellationToken.None);

        Assert.True(result.Success);
        var saved = await context.Categories.SingleAsync();
        Assert.Equal(result.Data, saved.Id);
        Assert.Equal(organization.Id, saved.OrganizationId);
        Assert.Equal("Safety", saved.Name);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenOrganizationDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateCategoryCommand(Guid.NewGuid(), "Safety", null), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenNameExistsInSameOrganization()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationAsync(context);
        context.Categories.Add(new Category(organization.Id, "Safety"));
        await context.SaveChangesAsync();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateCategoryCommand(organization.Id, "Safety", null), CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task Create_AllowsSameName_InDifferentOrganization()
    {
        await using var context = CreateContext();
        var first = await SeedOrganizationAsync(context, "Acme");
        var second = await SeedOrganizationAsync(context, "Contoso");
        context.Categories.Add(new Category(first.Id, "Safety"));
        await context.SaveChangesAsync();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new CreateCategoryCommand(second.Id, "Safety", null), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Update_ChangesNameDescriptionAndActiveFlag()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationAsync(context);
        var category = new Category(organization.Id, "Safety", "Old");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new UpdateCategoryCommand(organization.Id, category.Id, "Workplace Safety", "New", false),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Workplace Safety", result.Data!.Name);
        Assert.Equal("New", result.Data.Description);
        Assert.False(result.Data.IsActive);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenCategoryBelongsToAnotherOrganization()
    {
        await using var context = CreateContext();
        var owner = await SeedOrganizationAsync(context, "Acme");
        var other = await SeedOrganizationAsync(context, "Contoso");
        var category = new Category(owner.Id, "Safety");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new UpdateCategoryCommand(other.Id, category.Id, "Renamed", null, true), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenAnotherCategoryHasTheName()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationAsync(context);
        var category = new Category(organization.Id, "Safety");
        context.Categories.AddRange(category, new Category(organization.Id, "Compliance"));
        await context.SaveChangesAsync();
        var handler = new CategoryCommandHandlers(context, CreateMapper());

        var result = await handler.Handle(
            new UpdateCategoryCommand(organization.Id, category.Id, "Compliance", null, true), CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task GetCategories_ReturnsOnlyThatOrganizationsCategories_OrderedByName()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationAsync(context, "Acme");
        var other = await SeedOrganizationAsync(context, "Contoso");
        context.Categories.AddRange(
            new Category(organization.Id, "Safety"),
            new Category(organization.Id, "Compliance"),
            new Category(other.Id, "Other"));
        await context.SaveChangesAsync();
        var handler = new CategoryQueryHandlers(context, CreateMapper());

        var result = await handler.Handle(new GetCategoriesQuery(organization.Id), CancellationToken.None);

        Assert.Equal(["Compliance", "Safety"], result.Data!.Select(category => category.Name));
    }

    private static async Task<Organization> SeedOrganizationAsync(ApplicationDbContext context, string name = "Acme")
    {
        var organization = new Organization(name);
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization;
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IMapper CreateMapper() =>
        new MapperConfiguration(config => config.AddProfile<CategoryProfile>()).CreateMapper();
}
