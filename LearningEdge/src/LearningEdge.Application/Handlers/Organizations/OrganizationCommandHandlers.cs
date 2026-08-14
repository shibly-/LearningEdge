using AutoMapper;
using MediatR;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Domain.Entities.Organizations;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationCommandHandlers : IRequestHandler<CreateOrganizationCommand, Guid>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public OrganizationCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = new Organization(request.Name, request.Description);
        
        _appDbContext.Organizations.Add(organization);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        
        return organization.Id;
    }
}
