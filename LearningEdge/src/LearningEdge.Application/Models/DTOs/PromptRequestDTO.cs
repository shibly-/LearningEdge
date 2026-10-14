using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Application.Models.DTOs;

public record PromptRequestDTO(
    Guid Id,
    Guid TrainingId,
    Guid QuestionBankId,
    string PromptText,
    QuestionType QuestionType,
    int QuestionCount,
    DateTime RequestedOn,
    PromptStatus Status
);
