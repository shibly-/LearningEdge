using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;
using static LearningEdge.Domain.Common.Enums;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    UserRole Role,
    Guid OrganizationId
) : IRequest<Guid>;
