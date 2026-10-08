using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Categories;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Categories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Categories;

public class CategoryCommandHandlers :
    IRequestHandler<CreateCategoryCommand, Result<Guid>>,
    IRequestHandler<UpdateCategoryCommand, Result<CategoryDTO>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public CategoryCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<Guid>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var nameTaken = await _appDbContext.Categories
            .AnyAsync(
                category => category.OrganizationId == request.OrganizationId && category.Name == request.Name,
                cancellationToken);

        if (nameTaken)
        {
            return Result<Guid>.Conflict(NameTakenMessage(request.Name));
        }

        var category = new Category(request.OrganizationId, request.Name, request.Description ?? string.Empty, request.IsActive);
        _appDbContext.Categories.Add(category);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(NameTakenMessage(request.Name));
        }

        return Result<Guid>.Ok(category.Id);
    }

    public async Task<Result<CategoryDTO>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _appDbContext.Categories
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (category is null)
        {
            return Result<CategoryDTO>.NotFound(
                $"No category found with Id {request.Id} in organization {request.OrganizationId}.");
        }

        var nameTaken = await _appDbContext.Categories
            .AnyAsync(
                candidate => candidate.OrganizationId == request.OrganizationId
                    && candidate.Name == request.Name
                    && candidate.Id != request.Id,
                cancellationToken);

        if (nameTaken)
        {
            return Result<CategoryDTO>.Conflict(NameTakenMessage(request.Name));
        }

        category.Update(request.Name, request.Description ?? string.Empty, request.IsActive);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<CategoryDTO>.Conflict(NameTakenMessage(request.Name));
        }

        return Result<CategoryDTO>.Ok(_mapper.Map<CategoryDTO>(category));
    }

    private static string NameTakenMessage(string name) =>
        $"A category named {name} already exists in this organization.";
}
