using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;

/// <summary>Enables or disables an organization admin, including their Keycloak sign-in.</summary>
public record SetOrganizationAdminEnabledCommand(Guid Id, bool Enabled) : IRequest<Result<UserDTO>>;
