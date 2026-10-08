using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Organizations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationQueryHandlers :
    IRequestHandler<GetOrganizationByIdQuery, Result<OrganizationDTO>>,
    IRequestHandler<GetOrganizationsQuery, Result<IReadOnlyList<OrganizationDTO>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OrganizationQueryHandlers(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<OrganizationDTO>> Handle(
        GetOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDTO>.NotFound($"No organization found with Id {request.Id}.");
        }

        return Result<OrganizationDTO>.Ok(_mapper.Map<OrganizationDTO>(organization));
    }

    public async Task<Result<IReadOnlyList<OrganizationDTO>>> Handle(
        GetOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var organizations = await _context.Organizations
            .AsNoTracking()
            .OrderBy(organization => organization.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<OrganizationDTO> items = organizations
            .Select(organization => _mapper.Map<OrganizationDTO>(organization))
            .ToList();

        return Result<IReadOnlyList<OrganizationDTO>>.Ok(items);
    }
}
