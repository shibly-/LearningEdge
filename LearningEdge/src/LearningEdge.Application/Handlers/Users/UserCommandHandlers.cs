using AutoMapper;
using MediatR;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Users;

namespace LearningEdge.Application.Handlers.Users;

public class UserCommandHandlers: IRequestHandler<CreateUserCommand, Guid>      
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public UserCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }   

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);
        _appDbContext.Users.Add(user);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
