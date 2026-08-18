using LearningEdge.Application.Common.Results;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;
using static LearningEdge.Domain.Common.Enums;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,    
    UserRole Role,
    Guid OrganizationId
) : IRequest<Result<Guid>>;
