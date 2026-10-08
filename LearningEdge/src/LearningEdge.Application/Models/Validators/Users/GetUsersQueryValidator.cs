using FluentValidation;
using LearningEdge.Application.Models.Queries.Users;

namespace LearningEdge.Application.Models.Validators.Users;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("Organization id is invalid.");
        RuleForEach(x => x.Roles)
            .IsInEnum().WithMessage("Role is invalid.");
    }
}
