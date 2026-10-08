using MediatR;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Common.Results;

namespace LearningEdge.Application.Models.Queries.Users;

public record GetUsersByOrganizationQuery(Guid OrganizationId) : IRequest<Result<IReadOnlyList<UserDTO>>>;
