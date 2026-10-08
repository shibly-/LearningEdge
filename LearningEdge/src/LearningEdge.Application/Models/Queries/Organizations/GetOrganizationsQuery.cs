using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Organizations;

public record GetOrganizationsQuery : IRequest<Result<IReadOnlyList<OrganizationDTO>>>;
