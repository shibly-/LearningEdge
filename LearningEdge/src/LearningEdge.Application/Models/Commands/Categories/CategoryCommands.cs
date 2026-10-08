using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Categories;

public record CreateCategoryCommand(
    Guid OrganizationId,
    string Name,
    string? Description,
    bool IsActive = true
) : IRequest<Result<Guid>>;

public record UpdateCategoryCommand(
    Guid OrganizationId,
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
) : IRequest<Result<CategoryDTO>>;
