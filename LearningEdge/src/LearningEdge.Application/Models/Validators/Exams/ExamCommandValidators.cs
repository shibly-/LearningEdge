using FluentValidation;
using LearningEdge.Application.Models.Commands.Exams;

namespace LearningEdge.Application.Models.Validators.Exams;

public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.PreparedByUserId)
            .NotEmpty().WithMessage("Prepared by user id is required.");
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Exam title is required.")
            .MaximumLength(200).WithMessage("Exam title must not exceed 200 characters.");
    }
}

public class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
{
    public UpdateExamCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Exam id is required.");
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Exam title is required.")
            .MaximumLength(200).WithMessage("Exam title must not exceed 200 characters.");
    }
}

public class AddExamQuestionCommandValidator : AbstractValidator<AddExamQuestionCommand>
{
    public AddExamQuestionCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.ExamId)
            .NotEmpty().WithMessage("Exam id is required.");
        RuleFor(command => command.QuestionBankId)
            .NotEmpty().WithMessage("Question bank id is required.");
        RuleFor(command => command.QuestionId)
            .NotEmpty().WithMessage("Question id is required.");
    }
}

public class RemoveExamQuestionCommandValidator : AbstractValidator<RemoveExamQuestionCommand>
{
    public RemoveExamQuestionCommandValidator()
    {
        RuleFor(command => command.TrainingId)
            .NotEmpty().WithMessage("Training id is required.");
        RuleFor(command => command.ExamId)
            .NotEmpty().WithMessage("Exam id is required.");
        RuleFor(command => command.QuestionId)
            .NotEmpty().WithMessage("Question id is required.");
    }
}
