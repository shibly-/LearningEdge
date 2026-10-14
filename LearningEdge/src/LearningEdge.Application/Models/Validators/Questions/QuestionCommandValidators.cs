using FluentValidation;
using LearningEdge.Application.Models.Commands.Questions;
using LearningEdge.Application.Questions;

namespace LearningEdge.Application.Models.Validators.Questions;

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.QuestionBankId)
            .NotEmpty().WithMessage("Question bank id is required.");
        RuleFor(command => command.QuestionType)
            .IsInEnum().WithMessage("Question type is not supported.");
        RuleFor(command => command.Text)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(QuestionRules.MaxTextLength)
            .WithMessage($"Question text must not exceed {QuestionRules.MaxTextLength} characters.");
        RuleFor(command => command.Score)
            .InclusiveBetween(QuestionRules.MinScore, QuestionRules.MaxScore)
            .WithMessage($"Score must be between {QuestionRules.MinScore} and {QuestionRules.MaxScore}.");
    }
}

public class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    public UpdateQuestionCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.QuestionBankId)
            .NotEmpty().WithMessage("Question bank id is required.");
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Question id is required.");
        RuleFor(command => command.Text)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(QuestionRules.MaxTextLength)
            .WithMessage($"Question text must not exceed {QuestionRules.MaxTextLength} characters.");
        RuleFor(command => command.Score)
            .InclusiveBetween(QuestionRules.MinScore, QuestionRules.MaxScore)
            .WithMessage($"Score must be between {QuestionRules.MinScore} and {QuestionRules.MaxScore}.");
    }
}
