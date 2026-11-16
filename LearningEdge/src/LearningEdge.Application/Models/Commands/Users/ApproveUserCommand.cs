using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;

/// <summary>
/// Activates a pending registration and creates the Keycloak user.
/// <paramref name="ApproverEmail"/> comes from the bearer token.
/// <paramref name="ApproverHasPlatformRole"/> is the token's sysadmin realm role,
/// used when that operator has no LearningEdge user row yet.
/// </summary>
public record ApproveUserCommand(
    Guid UserId,
    string ApproverEmail,
    bool ApproverHasPlatformRole
) : IRequest<Result<UserDTO>>;
