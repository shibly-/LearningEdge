using AutoMapper;
using MediatR;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Organizations;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationQueryHandlers : IRequestHandler<GetOrganizationByIdQuery, OrganizationDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OrganizationQueryHandlers(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrganizationDTO> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations.FindAsync(new object[] { request.Id }, cancellationToken);

        if (organization == null)
        {
            return null!; // caller can handle NotFound
        }

        return _mapper.Map<OrganizationDTO>(organization);
    }
}
