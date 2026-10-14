using FluentValidation;
using LearningEdge.Application.Models.Commands.QuestionBanks;

namespace LearningEdge.Application.Models.Validators.QuestionBanks;

public class CreateQuestionBankCommandValidator : AbstractValidator<CreateQuestionBankCommand>
{
    public CreateQuestionBankCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Question bank title is required.")
            .MaximumLength(200).WithMessage("Question bank title must not exceed 200 characters.");
    }
}

public class UpdateQuestionBankCommandValidator : AbstractValidator<UpdateQuestionBankCommand>
{
    public UpdateQuestionBankCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Question bank id is required.");
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Question bank title is required.")
            .MaximumLength(200).WithMessage("Question bank title must not exceed 200 characters.");
    }
}
