using LearningEdge.Application.Handlers.Platform;
using LearningEdge.Application.Models.Queries.Platform;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class PlatformQueryHandlersTests
{
    [Fact]
    public async Task Handle_ShouldCountOrganizationsLearnersAndTrainings()
    {
        await using var context = CreateContext();
        var activeOrg = new Organization("Active org");
        var inactiveOrg = new Organization("Inactive org");
        inactiveOrg.SetActive(false);
        var deletedOrg = new Organization("Deleted org");
        deletedOrg.SoftDelete();

        var activeLearner = new User("Ada", "Learner", "ada@example.com", UserRole.Learner, activeOrg.Id);
        var pendingLearner = User.RegisterLearner("Bea", "Pending", "bea@example.com", activeOrg.Id);
        var inactiveLearner = new User("Cid", "Inactive", "cid@example.com", UserRole.Learner, activeOrg.Id);
        var instructor = new User("Dee", "Staff", "dee@example.com", UserRole.Instructor, activeOrg.Id);
        var deletedLearner = new User("Gone", "Learner", "gone@example.com", UserRole.Learner, activeOrg.Id);
        deletedLearner.SoftDelete();

        context.Organizations.AddRange(activeOrg, inactiveOrg, deletedOrg);
        context.Users.AddRange(activeLearner, pendingLearner, inactiveLearner, instructor, deletedLearner);
        context.Trainings.AddRange(
            new Training(activeOrg.Id, Guid.NewGuid(), "Live course", isActive: true),
            new Training(activeOrg.Id, Guid.NewGuid(), "Parked course", isActive: false));
        var deletedTraining = new Training(activeOrg.Id, Guid.NewGuid(), "Removed course");
        deletedTraining.SoftDelete();
        context.Trainings.Add(deletedTraining);
        await context.SaveChangesAsync();

        context.Entry(inactiveLearner).Property(user => user.ActivationStatus).CurrentValue =
            UserActivationStatus.Inactive;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new PlatformQueryHandlers(context).Handle(
            new GetPlatformSnapshotQuery(),
            CancellationToken.None);

        Assert.True(result.Success);
        var snapshot = result.Data!;
        Assert.Equal(1, snapshot.OrganizationsActive);
        Assert.Equal(1, snapshot.OrganizationsInactive);
        Assert.Equal(1, snapshot.LearnersActive);
        Assert.Equal(1, snapshot.LearnersPending);
        Assert.Equal(1, snapshot.LearnersInactive);
        Assert.Equal(1, snapshot.TrainingsActive);
        Assert.Equal(1, snapshot.TrainingsInactive);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
