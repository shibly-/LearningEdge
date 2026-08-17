using LearningEdge.Application.Common.Results;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Organizations;

public record CreateOrganizationCommand(
    string Name,
    string Description
) : IRequest<Result<Guid>>;
