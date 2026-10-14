using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Application.Models.DTOs;

public record QuestionDTO(
    Guid Id,
    Guid QuestionBankId,
    string Text,
    int Score,
    bool IsActive,
    QuestionType Type,
    IReadOnlyList<string> Options,
    IReadOnlyList<string> CorrectOptions,
    bool? CorrectValue,
    DateTime CreatedAt
);
