using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Users;

public class UserQueryHandlers :
    IRequestHandler<GetUserByIdQuery, Result<UserDTO>>,
    IRequestHandler<GetUsersByOrganizationQuery, Result<IReadOnlyList<UserDTO>>>,
    IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDTO>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserQueryHandlers(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<UserDTO>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (user is null)
        {
            return Result<UserDTO>.NotFound($"No user found with Id {request.Id}.");
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(user));
    }

    public async Task<Result<IReadOnlyList<UserDTO>>> Handle(
        GetUsersByOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        var organizationExists = await _context.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<IReadOnlyList<UserDTO>>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(user => user.OrganizationId == request.OrganizationId)
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserDTO>>.Ok(_mapper.Map<List<UserDTO>>(users));
    }

    public async Task<Result<IReadOnlyList<UserDTO>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        if (request.OrganizationId is { } organizationId)
        {
            var organizationExists = await _context.Organizations
                .AnyAsync(organization => organization.Id == organizationId, cancellationToken);

            if (!organizationExists)
            {
                return Result<IReadOnlyList<UserDTO>>.NotFound($"No organization found with Id {organizationId}.");
            }

            query = query.Where(user => user.OrganizationId == organizationId);
        }

        if (request.Roles.Count > 0)
        {
            var roles = request.Roles.Distinct().ToList();
            query = query.Where(user => roles.Contains(user.Role));
        }

        var users = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserDTO>>.Ok(_mapper.Map<List<UserDTO>>(users));
    }
}
