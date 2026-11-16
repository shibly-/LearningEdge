using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Users;
using LearningEdge.Application.Interfaces;
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

    [Fact]
    public async Task Register_ShouldStayPending_AndNotStoreThePassword()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector(), new RecordingKeycloak());
        var result = await handler.Handle(
            new RegisterLearnerCommand("Ada", "Lovelace", "ada@example.com", "Password1", organization.Id),
            CancellationToken.None);

        Assert.True(result.Success);
        var saved = await context.Users.SingleAsync();
        Assert.Equal(UserActivationStatus.Pending, saved.ActivationStatus);
        Assert.Equal(UserRole.Learner, saved.Role);
        Assert.Equal("enc:Password1", saved.PendingPasswordProtected);
        Assert.Null(saved.KeycloakUserId);
    }

    [Fact]
    public async Task Approve_ShouldCreateTheKeycloakUser_WhenOrgAdminOfTheSameOrganizationApproves()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var admin = new User("Ann", "Admin", "ann@example.com", UserRole.OrgAdmin, organization.Id);
        context.Organizations.Add(organization);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var registrar = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector(), new RecordingKeycloak());
        await registrar.Handle(
            new RegisterLearnerCommand("Ada", "Lovelace", "ada@example.com", "Password1", organization.Id),
            CancellationToken.None);
        var learnerId = await context.Users.Where(user => user.Email == "ada@example.com").Select(user => user.Id).SingleAsync();
        context.ChangeTracker.Clear();

        var keycloak = new RecordingKeycloak();
        var handler = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector(), keycloak);
        var result = await handler.Handle(
            new ApproveUserCommand(learnerId, "ann@example.com", false),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(UserActivationStatus.Active, result.Data!.ActivationStatus);
        Assert.Equal("ada@example.com", keycloak.Email);
        Assert.Equal("Password1", keycloak.Password);

        var saved = await context.Users.SingleAsync(user => user.Id == learnerId);
        Assert.Equal(UserActivationStatus.Active, saved.ActivationStatus);
        Assert.Equal("kc-1", saved.KeycloakUserId);
        Assert.Null(saved.PendingPasswordProtected);
    }

    [Fact]
    public async Task Approve_ShouldReturnForbidden_WhenTheApproverIsALearner()
    {
        await using var context = CreateContext();
        var organization = new Organization("Acme");
        var learner = new User("Grace", "Hopper", "grace@example.com", UserRole.Learner, organization.Id);
        context.Organizations.Add(organization);
        context.Users.Add(learner);
        await context.SaveChangesAsync();

        var registrar = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector());
        await registrar.Handle(
            new RegisterLearnerCommand("Ada", "Lovelace", "ada@example.com", "Password1", organization.Id),
            CancellationToken.None);
        var learnerId = await context.Users.Where(user => user.Email == "ada@example.com").Select(user => user.Id).SingleAsync();

        var keycloak = new RecordingKeycloak();
        var handler = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector(), keycloak);
        var result = await handler.Handle(
            new ApproveUserCommand(learnerId, "grace@example.com", false),
            CancellationToken.None);

        Assert.Equal(ResultErrorKind.Forbidden, result.Kind);
        Assert.Null(keycloak.Email);
    }

    [Fact]
    public async Task Approve_ShouldAllowASuperAdminFromAnotherOrganization()
    {
        await using var context = CreateContext();
        var acme = new Organization("Acme");
        var platform = new Organization("Platform");
        var sysAdmin = new User("Sue", "Admin", "sue@example.com", UserRole.SysAdmin, platform.Id);
        context.Organizations.AddRange(acme, platform);
        context.Users.Add(sysAdmin);
        await context.SaveChangesAsync();

        var registrar = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector());
        await registrar.Handle(
            new RegisterLearnerCommand("Ada", "Lovelace", "ada@example.com", "Password1", acme.Id),
            CancellationToken.None);
        var learnerId = await context.Users.Where(user => user.Email == "ada@example.com").Select(user => user.Id).SingleAsync();
        context.ChangeTracker.Clear();

        var handler = new UserCommandHandlers(context, CreateMapper(), new PrefixProtector(), new RecordingKeycloak());
        var result = await handler.Handle(
            new ApproveUserCommand(learnerId, "sue@example.com", true),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(UserActivationStatus.Active, result.Data!.ActivationStatus);
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
            var license = Environment.GetEnvironmentVariable("AUTOMAPPER_LICENSE_PATH");
            if (!string.IsNullOrWhiteSpace(license))
            {
                config.LicenseKey = license;
            }

            config.AddProfile<UserProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private sealed class PrefixProtector : IPendingCredentialProtector
    {
        public string Protect(string password) => "enc:" + password;

        public string Unprotect(string protectedPassword) => protectedPassword["enc:".Length..];
    }

    private sealed class RecordingKeycloak : IKeycloakUserDirectory
    {
        public string? Email { get; private set; }
        public string? Password { get; private set; }

        public Task<string> CreateLearnerAsync(KeycloakLearnerAccount account, CancellationToken cancellationToken)
        {
            Email = account.Email;
            Password = account.Password;
            return Task.FromResult("kc-1");
        }
    }
}
