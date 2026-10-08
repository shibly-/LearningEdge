using FluentValidation;
using LearningEdge.Application.Models.Commands.Organizations;

namespace LearningEdge.Application.Models.Validators.Organizations;

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization id is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(100).WithMessage("Organization name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Organization description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
