using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Users;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Tests.Domain.Entities;

public class OrganizationTests
{
    [Fact]
    public void Constructor_ShouldGenerateNewGuid()
    {
        var org = new Organization("Test Org");
        Assert.NotEqual(Guid.Empty, org.Id);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Organization(null));
    }

    [Fact]
    public void Constructor_ShouldSetNameCorrectly()
    {
        var org = new Organization("My Org");
        Assert.Equal("My Org", org.Name);
    }

    [Fact]
    public void Constructor_ShouldDefaultDescriptionToEmpty_WhenNotProvided()
    {
        var org = new Organization("My Org");
        Assert.Equal(string.Empty, org.Description);
    }

    [Fact]
    public void Constructor_ShouldSetDescription_WhenProvided()
    {
        var org = new Organization("My Org", "A sample description");
        Assert.Equal("A sample description", org.Description);
    }

    [Fact]
    public void Users_ShouldBeEmpty_OnInitialization()
    {
        var org = new Organization("My Org");
        Assert.Empty(org.Users);
    }

    [Fact]
    public void Users_ShouldBeReadOnly()
    {
        var org = new Organization("My Org");
        var collection = org.Users as ICollection<User>;
        Assert.NotNull(collection);
        Assert.True(collection.IsReadOnly);
    }
}
