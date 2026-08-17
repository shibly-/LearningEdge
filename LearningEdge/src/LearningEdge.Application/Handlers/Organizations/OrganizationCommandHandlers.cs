using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Domain.Entities.Organizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationCommandHandlers : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
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

        // Check if an organization with the same name already exists
        var existingOrganization = await _appDbContext.Organizations
            .FirstOrDefaultAsync(u => u.Name == request.Name, cancellationToken);

        if (existingOrganization != null)
        {
            // Generate custom exception
            return Result<Guid>.Fail($"Organization with name {request.Name} already exists with Id: {existingOrganization.Id}.");
        }

        var organization = new Organization(request.Name, request.Description);
        
        _appDbContext.Organizations.Add(organization);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Ok(organization.Id);
    }
}
