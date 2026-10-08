using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Organizations;

public record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string Description
) : IRequest<Result<OrganizationDTO>>;
