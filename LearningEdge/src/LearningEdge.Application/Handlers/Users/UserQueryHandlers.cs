using AutoMapper;
using MediatR;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;

namespace LearningEdge.Application.Handlers.Users;

public class UserQueryHandlers : IRequestHandler<GetUserByIdQuery, UserDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserQueryHandlers(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDTO> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);

        if (user == null)
        {
            return null!; // caller can handle NotFound
        }

        return _mapper.Map<UserDTO>(user);
    }
}
