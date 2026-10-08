using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Organizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationCommandHandlers :
    IRequestHandler<CreateOrganizationCommand, Result<Guid>>,
    IRequestHandler<UpdateOrganizationCommand, Result<OrganizationDTO>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public OrganizationCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var existingOrganization = await _appDbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(organization => organization.Name == request.Name, cancellationToken);

        if (existingOrganization is not null)
        {
            return Result<Guid>.Conflict(
                $"Organization with name {request.Name} already exists with Id: {existingOrganization.Id}.");
        }

        var organization = new Organization(request.Name, request.Description ?? string.Empty);
        _appDbContext.Organizations.Add(organization);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict($"Organization with name {request.Name} already exists.");
        }

        return Result<Guid>.Ok(organization.Id);
    }

    public async Task<Result<OrganizationDTO>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _appDbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDTO>.NotFound($"No organization found with Id {request.Id}.");
        }

        var nameTaken = await _appDbContext.Organizations
            .AnyAsync(
                candidate => candidate.Name == request.Name && candidate.Id != request.Id,
                cancellationToken);

        if (nameTaken)
        {
            return Result<OrganizationDTO>.Conflict($"Organization with name {request.Name} already exists.");
        }

        organization.Update(request.Name, request.Description ?? string.Empty);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<OrganizationDTO>.Conflict($"Organization with name {request.Name} already exists.");
        }

        return Result<OrganizationDTO>.Ok(_mapper.Map<OrganizationDTO>(organization));
    }
}
