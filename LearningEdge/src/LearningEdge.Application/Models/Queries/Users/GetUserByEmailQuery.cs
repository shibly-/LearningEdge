using MediatR;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Common.Results;

namespace LearningEdge.Application.Models.Queries.Users;

public record GetUserByEmailQuery(string Email) : IRequest<Result<UserDTO>>;
