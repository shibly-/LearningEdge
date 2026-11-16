using MediatR;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Common.Results;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Models.Queries.Users;

/// <summary>
/// Users across every organization. Every filter is optional: a null
/// organization means all organizations, an empty role list means all roles,
/// and a null activation status means pending and active.
/// </summary>
public record GetUsersQuery(
    Guid? OrganizationId,
    IReadOnlyList<UserRole> Roles,
    UserActivationStatus? ActivationStatus = null)
    : IRequest<Result<IReadOnlyList<UserDTO>>>;
