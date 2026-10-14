namespace LearningEdge.Application.Models.DTOs;

public record QuestionBankDTO(
    Guid Id,
    Guid TrainingId,
    string Title,
    bool IsActive,
    DateTime CreatedAt
);
