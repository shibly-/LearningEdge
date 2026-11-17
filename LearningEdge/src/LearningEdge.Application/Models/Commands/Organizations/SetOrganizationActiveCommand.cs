using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Organizations;

public record SetOrganizationActiveCommand(Guid Id, bool IsActive) : IRequest<Result<OrganizationDTO>>;
