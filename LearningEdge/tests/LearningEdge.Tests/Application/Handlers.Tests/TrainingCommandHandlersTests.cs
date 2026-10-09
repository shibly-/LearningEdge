using System.Text;
using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Trainings;
using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Application.Models.Queries.Trainings;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Tests.Common;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class TrainingCommandHandlersTests
{
    private readonly InMemoryFileStorage _storage = new();

    [Fact]
    public async Task Create_SavesTraining_WhenCategoryExists()
    {
        await using var context = CreateContext();
        var (_, category) = await SeedCategoryAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new CreateTrainingCommand(category.Id, "Fire Safety", "Basics"), CancellationToken.None);

        Assert.True(result.Success);
        var saved = await context.Trainings.SingleAsync();
        Assert.Equal(category.Id, saved.CategoryId);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new CreateTrainingCommand(Guid.NewGuid(), "Fire Safety", null), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenNameExistsInCategory()
    {
        await using var context = CreateContext();
        var (_, category) = await SeedCategoryAsync(context);
        context.Trainings.Add(new Training(category.Id, "Fire Safety"));
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new CreateTrainingCommand(category.Id, "Fire Safety", null), CancellationToken.None);

        Assert.Equal(ResultErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public async Task Update_ChangesTraining_AndCanDeactivateIt()
    {
        await using var context = CreateContext();
        var (_, category) = await SeedCategoryAsync(context);
        var training = new Training(category.Id, "Fire Safety");
        context.Trainings.Add(training);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UpdateTrainingCommand(category.Id, training.Id, "Fire Safety 101", "Updated", false),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Fire Safety 101", result.Data!.Name);
        Assert.False(result.Data.IsActive);
        Assert.Empty(result.Data.Files);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTrainingIsInAnotherCategory()
    {
        await using var context = CreateContext();
        var (organization, category) = await SeedCategoryAsync(context);
        var otherCategory = new Category(organization.Id, "Other");
        var training = new Training(category.Id, "Fire Safety");
        context.Categories.Add(otherCategory);
        context.Trainings.Add(training);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UpdateTrainingCommand(otherCategory.Id, training.Id, "Renamed", null, true), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Theory]
    [InlineData(UserRole.OrgAdmin)]
    [InlineData(UserRole.SysAdmin)]
    public async Task Upload_StoresFiles_ForAdmins(UserRole role)
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, role, organization.Id);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id,
            [
                Upload("guide.pdf", "%PDF-1.7 content"),
                Upload("notes.txt", "plain text notes"),
                Upload("handout.docx", "PK\u0003\u0004 zip body")
            ]),
            CancellationToken.None);

        Assert.True(result.Success, result.Error);
        Assert.Equal(3, result.Data!.Count);
        Assert.Equal(3, _storage.Files.Count);
        Assert.Equal(3, await context.TrainingFiles.CountAsync(file => file.TrainingId == training.Id));

        var pdf = result.Data.Single(file => file.FileName == "guide.pdf");
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.Equal(admin.Id, pdf.UploadedByUserId);
    }

    [Fact]
    public async Task Upload_IsForbidden_ForLearner()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var learner = await SeedUserAsync(context, UserRole.Learner, organization.Id);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, learner.Id, [Upload("guide.pdf", "%PDF-1.7")]),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Forbidden, result.Kind);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_IsForbidden_ForOrgAdminOfAnotherOrganization()
    {
        await using var context = CreateContext();
        var (_, category, training) = await SeedTrainingAsync(context);
        var otherOrganization = new Organization("Contoso");
        context.Organizations.Add(otherOrganization);
        var outsider = await SeedUserAsync(context, UserRole.OrgAdmin, otherOrganization.Id);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, outsider.Id, [Upload("guide.pdf", "%PDF-1.7")]),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task Upload_IsForbidden_ForUnknownUser()
    {
        await using var context = CreateContext();
        var (_, category, training) = await SeedTrainingAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, Guid.NewGuid(), [Upload("guide.pdf", "%PDF-1.7")]),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task Upload_ReturnsNotFound_WhenTrainingIsNotInCategory()
    {
        await using var context = CreateContext();
        var (organization, _, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(Guid.NewGuid(), training.Id, admin.Id, [Upload("guide.pdf", "%PDF-1.7")]),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Upload_RejectsAllFiles_WhenOneDoesNotMatchItsExtension()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id,
            [
                Upload("guide.pdf", "%PDF-1.7"),
                Upload("fake.pdf", "MZ this is an executable")
            ]),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Validation, result.Kind);
        Assert.Contains("fake.pdf", result.Error);
        Assert.Empty(_storage.Files);
        Assert.Equal(0, await context.TrainingFiles.CountAsync());
    }

    [Fact]
    public async Task Remove_SoftDeletesTheRow_AndDeletesStoredBytes()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var handler = CreateHandler(context);
        var uploaded = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id,
            [
                Upload("notes.txt", "hello"),
                Upload("guide.pdf", "%PDF-1.7")
            ]),
            CancellationToken.None);
        var target = uploaded.Data!.Single(file => file.FileName == "notes.txt");
        context.ChangeTracker.Clear();

        var result = await handler.Handle(
            new RemoveTrainingFileCommand(category.Id, training.Id, target.Id, admin.Id),
            CancellationToken.None);

        Assert.True(result.Success, result.Error);
        Assert.Equal(target.Id, result.Data);
        context.ChangeTracker.Clear();

        Assert.Equal(1, await context.TrainingFiles.CountAsync(file => file.TrainingId == training.Id));
        var deleted = await context.TrainingFiles.IgnoreQueryFilters().SingleAsync(file => file.Id == target.Id);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.UpdatedAt);
        Assert.Single(_storage.Files);
        Assert.DoesNotContain(_storage.Files.Keys, key => key == deleted.StorageKey);

        var loaded = await new TrainingQueryHandlers(context, CreateMapper())
            .Handle(new GetTrainingByIdQuery(category.Id, training.Id), CancellationToken.None);
        Assert.DoesNotContain(loaded.Data!.Files, file => file.Id == target.Id);
        Assert.Contains(loaded.Data.Files, file => file.FileName == "guide.pdf");
    }

    [Fact]
    public async Task Remove_IsForbidden_ForLearner()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var learner = await SeedUserAsync(context, UserRole.Learner, organization.Id);
        var handler = CreateHandler(context);
        var uploaded = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id, [Upload("notes.txt", "hello")]),
            CancellationToken.None);
        var fileId = uploaded.Data!.Single().Id;
        context.ChangeTracker.Clear();

        var result = await handler.Handle(
            new RemoveTrainingFileCommand(category.Id, training.Id, fileId, learner.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Forbidden, result.Kind);
        Assert.Equal(1, await context.TrainingFiles.CountAsync());
        Assert.Single(_storage.Files);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenFileIsOnAnotherTraining()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var other = new Training(category.Id, "Other course");
        context.Trainings.Add(other);
        await context.SaveChangesAsync();
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var handler = CreateHandler(context);
        var uploaded = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id, [Upload("notes.txt", "hello")]),
            CancellationToken.None);
        var fileId = uploaded.Data!.Single().Id;
        context.ChangeTracker.Clear();

        var result = await handler.Handle(
            new RemoveTrainingFileCommand(category.Id, other.Id, fileId, admin.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
        Assert.Equal(1, await context.TrainingFiles.CountAsync());
        Assert.Single(_storage.Files);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenTrainingIsNotInCategory()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        var handler = CreateHandler(context);
        var uploaded = await handler.Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id, [Upload("notes.txt", "hello")]),
            CancellationToken.None);
        context.ChangeTracker.Clear();

        var result = await handler.Handle(
            new RemoveTrainingFileCommand(Guid.NewGuid(), training.Id, uploaded.Data!.Single().Id, admin.Id),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
        Assert.Single(_storage.Files);
    }

    [Fact]
    public async Task GetById_IncludesUploadedFiles()
    {
        await using var context = CreateContext();
        var (organization, category, training) = await SeedTrainingAsync(context);
        var admin = await SeedUserAsync(context, UserRole.OrgAdmin, organization.Id);
        await CreateHandler(context).Handle(
            new UploadTrainingFilesCommand(category.Id, training.Id, admin.Id, [Upload("notes.txt", "hello")]),
            CancellationToken.None);
        context.ChangeTracker.Clear();

        var result = await new TrainingQueryHandlers(context, CreateMapper())
            .Handle(new GetTrainingByIdQuery(category.Id, training.Id), CancellationToken.None);

        var file = Assert.Single(result.Data!.Files);
        Assert.Equal("notes.txt", file.FileName);
        Assert.Equal("text/plain", file.ContentType);
    }

    private TrainingCommandHandlers CreateHandler(ApplicationDbContext context) =>
        new(context, CreateMapper(), _storage);

    private static TrainingFileUpload Upload(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new TrainingFileUpload(fileName, "application/octet-stream", bytes.Length, new MemoryStream(bytes));
    }

    private static async Task<(Organization, Category)> SeedCategoryAsync(ApplicationDbContext context)
    {
        var organization = new Organization("Acme");
        var category = new Category(organization.Id, "Safety");
        context.Organizations.Add(organization);
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return (organization, category);
    }

    private static async Task<(Organization, Category, Training)> SeedTrainingAsync(ApplicationDbContext context)
    {
        var (organization, category) = await SeedCategoryAsync(context);
        var training = new Training(category.Id, "Fire Safety");
        context.Trainings.Add(training);
        await context.SaveChangesAsync();
        return (organization, category, training);
    }

    private static async Task<User> SeedUserAsync(ApplicationDbContext context, UserRole role, Guid organizationId)
    {
        var user = new User("Ada", "Lovelace", $"{Guid.NewGuid():N}@acme.test", role, organizationId);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IMapper CreateMapper() =>
        new MapperConfiguration(config => config.AddProfile<TrainingProfile>()).CreateMapper();
}
