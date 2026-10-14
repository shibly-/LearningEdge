using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.ValueObjects;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Questions;

public record CreateQuestionCommand(
    Guid TrainingId,
    Guid QuestionBankId,
    QuestionType QuestionType,
    string Text,
    int Score,
    bool IsActive,
    IReadOnlyList<string>? Options,
    IReadOnlyList<string>? CorrectOptions,
    bool? CorrectValue
) : IRequest<Result<Guid>>;

public record UpdateQuestionCommand(
    Guid TrainingId,
    Guid QuestionBankId,
    Guid Id,
    string Text,
    int Score,
    bool IsActive,
    IReadOnlyList<string>? Options,
    IReadOnlyList<string>? CorrectOptions,
    bool? CorrectValue
) : IRequest<Result<QuestionDTO>>;
