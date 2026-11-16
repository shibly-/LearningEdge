using FluentValidation;
using LearningEdge.Application.Models.Commands.Users;

namespace LearningEdge.Application.Models.Validators.Users;

public class ApproveUserCommandValidator : AbstractValidator<ApproveUserCommand>
{
    public ApproveUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
        RuleFor(x => x.ApproverEmail)
            .EmailAddress().WithMessage("Approver email is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApproverEmail));
    }
}
