using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Exams;

public record CreateExamCommand(
    Guid TrainingId,
    Guid PreparedByUserId,
    string Title,
    bool IsActive = true
) : IRequest<Result<Guid>>;

public record UpdateExamCommand(
    Guid TrainingId,
    Guid Id,
    string Title,
    bool IsActive
) : IRequest<Result<ExamDTO>>;

public record AddExamQuestionCommand(
    Guid TrainingId,
    Guid ExamId,
    Guid QuestionBankId,
    Guid QuestionId
) : IRequest<Result<Guid>>;

public record RemoveExamQuestionCommand(
    Guid TrainingId,
    Guid ExamId,
    Guid QuestionId
) : IRequest<Result<Guid>>;
