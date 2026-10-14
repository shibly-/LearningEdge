namespace LearningEdge.Application.Models.DTOs;

public record ExamDTO(
    Guid Id,
    Guid TrainingId,
    Guid PreparedByUserId,
    string Title,
    bool IsActive,
    DateTime CreatedAt
);
