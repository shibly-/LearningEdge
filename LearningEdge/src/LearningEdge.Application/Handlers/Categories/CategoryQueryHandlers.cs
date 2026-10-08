using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Categories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Categories;

public class CategoryQueryHandlers :
    IRequestHandler<GetCategoryByIdQuery, Result<CategoryDTO>>,
    IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDTO>>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public CategoryQueryHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDTO>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _appDbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.OrganizationId == request.OrganizationId,
                cancellationToken);

        return category is null
            ? Result<CategoryDTO>.NotFound($"No category found with Id {request.Id} in organization {request.OrganizationId}.")
            : Result<CategoryDTO>.Ok(_mapper.Map<CategoryDTO>(category));
    }

    public async Task<Result<IReadOnlyList<CategoryDTO>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<IReadOnlyList<CategoryDTO>>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var categories = await _appDbContext.Categories
            .AsNoTracking()
            .Where(category => category.OrganizationId == request.OrganizationId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CategoryDTO>>.Ok(_mapper.Map<List<CategoryDTO>>(categories));
    }
}
