using MediatR;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Common.Results;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Models.Queries.Users;

/// <summary>
/// Users across every organization. Both filters are optional: a null
/// organization means all organizations, an empty role list means all roles.
/// </summary>
public record GetUsersQuery(Guid? OrganizationId, IReadOnlyList<UserRole> Roles)
    : IRequest<Result<IReadOnlyList<UserDTO>>>;
