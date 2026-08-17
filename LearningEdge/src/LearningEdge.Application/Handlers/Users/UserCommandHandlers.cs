using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Users;

public class UserCommandHandlers: IRequestHandler<CreateUserCommand, Result<Guid>>      
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
        // Check if a user with the same email already exists in the same organization
        var existingUser = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email
                                   && u.OrganizationId == request.OrganizationId,
                                 cancellationToken);

        if (existingUser != null)
        {
            // Generate custom exception
            return Result<Guid>.Fail($"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        var user = new User(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);
        _appDbContext.Users.Add(user);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Ok(user.Id);
    }
}
