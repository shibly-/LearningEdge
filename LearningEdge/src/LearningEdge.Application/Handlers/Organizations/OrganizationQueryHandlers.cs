using AutoMapper;
using MediatR;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Organizations;
using LearningEdge.Application.Common.Results;

namespace LearningEdge.Application.Handlers.Organizations;

public class OrganizationQueryHandlers : IRequestHandler<GetOrganizationByIdQuery, Result<OrganizationDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public OrganizationQueryHandlers(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<OrganizationDTO>> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations.FindAsync(new object[] { request.Id }, cancellationToken);

        if (organization == null)
        {
            // Generate custom exception
            return Result<OrganizationDTO>.Fail($"No organization found with Id {request.Id}.");
        }

        return Result<OrganizationDTO>.Ok(_mapper.Map<OrganizationDTO>(organization));
    }
}
