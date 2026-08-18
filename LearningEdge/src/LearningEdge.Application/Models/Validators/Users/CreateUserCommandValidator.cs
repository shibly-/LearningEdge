using FluentValidation;
using LearningEdge.Application.Models.Commands.Users;

namespace LearningEdge.Application.Models.Validators.Users;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>  
{
    public CreateUserCommandValidator()
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
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.");
    }
}
