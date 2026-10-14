using FluentValidation;
using LearningEdge.Application.Models.Commands.PromptRequests;

namespace LearningEdge.Application.Models.Validators.PromptRequests;

public class SubmitPromptRequestCommandValidator : AbstractValidator<SubmitPromptRequestCommand>
{
    public SubmitPromptRequestCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.QuestionBankId)
            .NotEmpty().WithMessage("Question bank id is required.");
        RuleFor(command => command.PromptText)
            .NotEmpty().WithMessage("Prompt text is required.")
            .MaximumLength(4000).WithMessage("Prompt text must not exceed 4000 characters.");
        RuleFor(command => command.QuestionType)
            .IsInEnum().WithMessage("Question type is not supported.");
        RuleFor(command => command.QuestionCount)
            .InclusiveBetween(1, 50).WithMessage("Question count must be between 1 and 50.");
    }
}
