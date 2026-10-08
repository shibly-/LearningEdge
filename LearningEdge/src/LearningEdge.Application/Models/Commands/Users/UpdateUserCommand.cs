using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Models.Commands.Users;

public record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    Guid OrganizationId
) : IRequest<Result<UserDTO>>;
