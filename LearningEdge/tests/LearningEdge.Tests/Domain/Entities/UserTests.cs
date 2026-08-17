using LearningEdge.Domain.Entities.Users;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldGenerateNewGuid()
    {
        var user = new User("Harry", "Potter", "harry.potter@test.com", UserRole.Instructor, Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenFirstNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new User(null, "Harry", "harry.potter@test.com", UserRole.Instructor, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_ShouldDefaultLastNameToEmpty_WhenNull()
    {
        var user = new User("Harry", null, "harry.potter@test.com", UserRole.Instructor, Guid.NewGuid());
        Assert.Equal(string.Empty, user.LastName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenEmailIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new User("Harry", "Potter", null, UserRole.Instructor, Guid.NewGuid()));
    }

    [Fact]
    public void CreateUser_ShouldDefaultToLearnerRole_WhenInvalidRoleProvided()
    {
        // Arrange
        var invalidRole = (UserRole)999;
        var user = new User("Harry", "Potter", "harry.potter@test.com", invalidRole, Guid.NewGuid());

        // Act & Assert
        Assert.Equal(UserRole.Learner, user.Role);
    }

    [Theory]
    [InlineData(UserRole.Learner)]
    [InlineData(UserRole.Instructor)]
    [InlineData(UserRole.OrgAdmin)]
    [InlineData(UserRole.SysAdmin)]
    public void Constructor_ShouldAcceptValidRoles(UserRole role)
    {
        var user = new User("Harry", "Potter", "harry.potter@test.com", role, Guid.NewGuid());
        Assert.Equal(role, user.Role);
    }
}
