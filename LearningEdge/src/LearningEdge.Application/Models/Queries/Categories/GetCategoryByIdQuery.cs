using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Categories;

public record GetCategoryByIdQuery(Guid OrganizationId, Guid Id) : IRequest<Result<CategoryDTO>>;

public record GetCategoriesQuery(Guid OrganizationId) : IRequest<Result<IReadOnlyList<CategoryDTO>>>;
