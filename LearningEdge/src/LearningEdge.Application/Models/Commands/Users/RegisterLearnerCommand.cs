using LearningEdge.Application.Common.Results;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Users;

/// <summary>
/// Public self-registration. The role is always Learner and the account stays
/// pending; the client cannot choose either.
/// </summary>
public record RegisterLearnerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid OrganizationId
) : IRequest<Result<Guid>>;
