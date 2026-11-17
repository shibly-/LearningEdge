using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Handlers.Users;

public class UserQueryHandlers :
    IRequestHandler<GetUserByIdQuery, Result<UserDTO>>,
    IRequestHandler<GetUserByEmailQuery, Result<UserDTO>>,
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

    public async Task<Result<UserDTO>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        // Email is unique per organization, not globally, so the same address can
        // exist in two tenants. Take two to tell "one match" from "ambiguous".
        var matches = await _context.Users
            .AsNoTracking()
            .Where(candidate => candidate.Email == email)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            return Result<UserDTO>.NotFound($"No user found with email {email}.");
        }

        // Choosing one would silently scope the caller to an arbitrary organization.
        if (matches.Count > 1)
        {
            return Result<UserDTO>.Conflict(
                $"The email {email} belongs to more than one organization. Each sign-in must map to a single user.");
        }

        if (matches[0].ActivationStatus == UserActivationStatus.Pending)
        {
            return Result<UserDTO>.Forbidden(
                "This account is waiting for an administrator to approve it.");
        }

        if (matches[0].ActivationStatus == UserActivationStatus.Inactive)
        {
            return Result<UserDTO>.Forbidden(
                "This account has been disabled. Ask a system admin to enable it.");
        }

        if (matches[0].Role != UserRole.SysAdmin)
        {
            var organizationActive = await _context.Organizations
                .AsNoTracking()
                .AnyAsync(
                    organization => organization.Id == matches[0].OrganizationId && organization.IsActive,
                    cancellationToken);

            if (!organizationActive)
            {
                return Result<UserDTO>.Forbidden(
                    "This organization has been disabled. Ask a system admin to enable it.");
            }
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(matches[0]));
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

        if (request.ActivationStatus is { } activationStatus)
        {
            query = query.Where(user => user.ActivationStatus == activationStatus);
        }

        var users = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserDTO>>.Ok(_mapper.Map<List<UserDTO>>(users));
    }
}
