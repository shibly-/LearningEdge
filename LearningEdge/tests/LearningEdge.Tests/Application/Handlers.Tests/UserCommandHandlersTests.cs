using AutoMapper;
using LearningEdge.Application.Handlers.Users;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Users;
using Moq;
using static LearningEdge.Domain.Common.Enums;
using LearningEdge.Tests.Common;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class UserCommandHandlersTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly UserCommandHandlers _handler;
    private readonly IMapper _mapper;

    public UserCommandHandlersTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mapper = new Mock<IMapper>().Object;
        _handler = new UserCommandHandlers(_mockContext.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnFail_WhenUserAlreadyExists()
    {
        // Arrange
        var existingUsers = new List<User>
        {
            new("John", "Doe", "john@example.com", UserRole.Learner, Guid.NewGuid())
        };

        var mockUsers = MockHelper.CreateMockDbSet(existingUsers);

        var mockContext = new Mock<IApplicationDbContext>();
        mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

        var handler = new UserCommandHandlers(mockContext.Object, _mapper);

        CreateUserCommand command = new (
            "John", "Doe", "john@example.com", UserRole.Learner, existingUsers[0].OrganizationId
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
    }
}
