using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Application.Questions;

public static class QuestionRules
{
    public const int MaxTextLength = 2000;
    public const int MinScore = 1;
    public const int MaxScore = 100;
    public const int MinOptions = 2;
    public const int MaxOptions = 8;
    public const int MaxOptionLength = 300;
}

public sealed record QuestionDraft(
    string Text,
    int Score,
    bool IsActive,
    IReadOnlyList<Option> Options,
    IReadOnlyList<Option> CorrectOptions,
    bool? CorrectValue);

public static class QuestionEditor
{
    public static Result<QuestionDraft> Draft(
        QuestionType type,
        string text,
        int score,
        bool isActive,
        IReadOnlyList<string>? options,
        IReadOnlyList<string>? correctOptions,
        bool? correctValue)
    {
        if (!Enum.IsDefined(type))
        {
            return Result<QuestionDraft>.Validation("Question type is not supported.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<QuestionDraft>.Validation("Question text is required.");
        }

        var normalizedText = text.Trim();
        if (normalizedText.Length > QuestionRules.MaxTextLength)
        {
            return Result<QuestionDraft>.Validation($"Question text must not exceed {QuestionRules.MaxTextLength} characters.");
        }

        if (score < QuestionRules.MinScore || score > QuestionRules.MaxScore)
        {
            return Result<QuestionDraft>.Validation(
                $"Score must be between {QuestionRules.MinScore} and {QuestionRules.MaxScore}.");
        }

        if (type == QuestionType.TrueFalse)
        {
            if (correctValue is null)
            {
                return Result<QuestionDraft>.Validation("A true/false question needs a correct answer.");
            }

            return Result<QuestionDraft>.Ok(new QuestionDraft(normalizedText, score, isActive, [], [], correctValue));
        }

        var optionTexts = Distinct(options);
        if (optionTexts.Count < QuestionRules.MinOptions || optionTexts.Count > QuestionRules.MaxOptions)
        {
            return Result<QuestionDraft>.Validation(
                $"Provide between {QuestionRules.MinOptions} and {QuestionRules.MaxOptions} options.");
        }

        if (optionTexts.Any(option => option.Length > QuestionRules.MaxOptionLength))
        {
            return Result<QuestionDraft>.Validation(
                $"Each option must be {QuestionRules.MaxOptionLength} characters or fewer.");
        }

        var wanted = Distinct(correctOptions);
        var matched = optionTexts
            .Where(option => wanted.Any(correct => string.Equals(option, correct, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matched.Count != wanted.Count)
        {
            return Result<QuestionDraft>.Validation("Every correct answer must match one of the options.");
        }

        if (type == QuestionType.Mcq && matched.Count != 1)
        {
            return Result<QuestionDraft>.Validation("A multiple choice question needs exactly one correct option.");
        }

        if (type == QuestionType.MultipleSelection && matched.Count == 0)
        {
            return Result<QuestionDraft>.Validation("Select at least one correct option.");
        }

        return Result<QuestionDraft>.Ok(new QuestionDraft(
            normalizedText,
            score,
            isActive,
            optionTexts.Select(option => new Option(option)).ToList(),
            matched.Select(option => new Option(option)).ToList(),
            null));
    }

    public static Question Create(QuestionType type, Guid questionBankId, QuestionDraft draft) => type switch
    {
        QuestionType.Mcq => new McqQuestion(
            draft.Text,
            questionBankId,
            draft.Score,
            draft.Options,
            new SingleChoiceAnswer(draft.CorrectOptions.Single()),
            draft.IsActive),
        QuestionType.TrueFalse => new TrueFalseQuestion(
            draft.Text,
            questionBankId,
            draft.Score,
            new TrueFalseAnswer(draft.CorrectValue ?? false),
            draft.IsActive),
        QuestionType.MultipleSelection => new MultipleSelectionQuestion(
            draft.Text,
            questionBankId,
            draft.Score,
            draft.Options,
            new MultipleChoiceAnswer(draft.CorrectOptions),
            draft.IsActive),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Question type is not supported.")
    };

    public static string? Apply(Question question, QuestionDraft draft)
    {
        try
        {
            switch (question)
            {
                case McqQuestion mcq:
                    mcq.Update(
                        draft.Text,
                        draft.Score,
                        draft.IsActive,
                        draft.Options,
                        new SingleChoiceAnswer(draft.CorrectOptions.Single()));
                    return null;
                case TrueFalseQuestion trueFalse:
                    trueFalse.Update(
                        draft.Text,
                        draft.Score,
                        draft.IsActive,
                        new TrueFalseAnswer(draft.CorrectValue ?? false));
                    return null;
                case MultipleSelectionQuestion multiple:
                    multiple.Update(
                        draft.Text,
                        draft.Score,
                        draft.IsActive,
                        draft.Options,
                        new MultipleChoiceAnswer(draft.CorrectOptions));
                    return null;
                default:
                    return "This question type cannot be edited.";
            }
        }
        catch (ArgumentException exception)
        {
            return exception.Message;
        }
    }

    public static QuestionType TypeOf(Question question) => question switch
    {
        McqQuestion => QuestionType.Mcq,
        TrueFalseQuestion => QuestionType.TrueFalse,
        MultipleSelectionQuestion => QuestionType.MultipleSelection,
        _ => throw new InvalidOperationException($"Unsupported question type {question.GetType().Name}.")
    };

    public static QuestionDTO ToDto(Question question) => question switch
    {
        McqQuestion mcq => Map(
            mcq,
            QuestionType.Mcq,
            mcq.Options,
            [mcq.Answer.CorrectOption.Text],
            null),
        TrueFalseQuestion trueFalse => Map(
            trueFalse,
            QuestionType.TrueFalse,
            [],
            [],
            trueFalse.Answer.CorrectValue),
        MultipleSelectionQuestion multiple => Map(
            multiple,
            QuestionType.MultipleSelection,
            multiple.Options,
            multiple.Answer.CorrectOptions.Select(option => option.Text).ToList(),
            null),
        _ => throw new InvalidOperationException($"Unsupported question type {question.GetType().Name}.")
    };

    private static QuestionDTO Map(
        Question question,
        QuestionType type,
        IReadOnlyCollection<Option> options,
        IReadOnlyList<string> correctOptions,
        bool? correctValue) => new(
        question.Id,
        question.QuestionBankId,
        question.Text,
        question.Score,
        question.IsActive,
        type,
        options.Select(option => option.Text).ToList(),
        correctOptions,
        correctValue,
        question.CreatedAt);

    private static List<string> Distinct(IReadOnlyList<string>? values)
    {
        var cleaned = new List<string>();
        foreach (var value in values ?? [])
        {
            var text = value.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (cleaned.Any(existing => string.Equals(existing, text, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            cleaned.Add(text);
        }

        return cleaned;
    }
}
