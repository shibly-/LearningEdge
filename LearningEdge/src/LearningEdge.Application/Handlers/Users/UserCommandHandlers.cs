using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Users;

public class UserCommandHandlers :
    IRequestHandler<CreateUserCommand, Result<Guid>>,
    IRequestHandler<UpdateUserCommand, Result<UserDTO>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public UserCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<Guid>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var existingUser = await _appDbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email == request.Email && user.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<Guid>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        var user = new User(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);
        _appDbContext.Users.Add(user);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<UserDTO>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (user is null)
        {
            return Result<UserDTO>.NotFound($"No user found with Id {request.Id}.");
        }

        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<UserDTO>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var emailTaken = await _appDbContext.Users
            .AnyAsync(
                candidate => candidate.Email == request.Email
                    && candidate.OrganizationId == request.OrganizationId
                    && candidate.Id != request.Id,
                cancellationToken);

        if (emailTaken)
        {
            return Result<UserDTO>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        user.Update(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<UserDTO>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(user));
    }
}
