using MediatR;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Common.Results;

namespace LearningEdge.Application.Models.Queries.Users;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDTO>>;
