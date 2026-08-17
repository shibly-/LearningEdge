using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using LearningEdge.Domain.Entities.Organizations;
using MediatR;

namespace LearningEdge.Application.Handlers.Users;

public class UserQueryHandlers : IRequestHandler<GetUserByIdQuery, Result<UserDTO>>
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
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);

        if (user == null)
        {
            // Generate custom exception
            return Result<UserDTO>.Fail($"No user found with Id {request.Id}.");
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(user));
    }
}
