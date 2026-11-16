using LearningEdge.Application.Common.Results;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;

/// <summary>
/// Appoints an organization admin and creates the matching Keycloak user,
/// so the person can sign in. System admin only.
/// </summary>
public record CreateOrganizationAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid OrganizationId
) : IRequest<Result<Guid>>;
