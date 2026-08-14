using FluentValidation;
using LearningEdge.Application.Models.Commands.Organizations;


namespace LearningEdge.Application.Models.Validators.Organizations;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(100).WithMessage("Organization name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Organization description must not exceed 500 characters.");
    }
}
