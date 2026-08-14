using MediatR;
using LearningEdge.Application.Models.DTOs;

namespace LearningEdge.Application.Models.Queries.Users;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDTO>;
