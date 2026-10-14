using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.QuestionBanks;

public record CreateQuestionBankCommand(
    Guid TrainingId,
    string Title,
    bool IsActive = true
) : IRequest<Result<Guid>>;

public record UpdateQuestionBankCommand(
    Guid TrainingId,
    Guid Id,
    string Title,
    bool IsActive
) : IRequest<Result<QuestionBankDTO>>;
