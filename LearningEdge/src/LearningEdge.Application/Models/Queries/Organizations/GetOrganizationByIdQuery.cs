using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Organizations;

public record GetOrganizationByIdQuery(Guid Id) : IRequest<Result<OrganizationDTO>>;


