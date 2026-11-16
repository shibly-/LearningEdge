using FluentValidation;
using LearningEdge.Application.Models.Commands.Users;

namespace LearningEdge.Application.Models.Validators.Users;

public class RegisterLearnerCommandValidator : AbstractValidator<RegisterLearnerCommand>
{
    public RegisterLearnerCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");
        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization id is required.");
    }
}
