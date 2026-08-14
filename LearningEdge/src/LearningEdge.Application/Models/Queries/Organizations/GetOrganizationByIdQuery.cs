using MediatR;
using LearningEdge.Application.Models.DTOs;

namespace LearningEdge.Application.Models.Queries.Organizations;

public record GetOrganizationByIdQuery(Guid Id) : IRequest<OrganizationDTO>;


