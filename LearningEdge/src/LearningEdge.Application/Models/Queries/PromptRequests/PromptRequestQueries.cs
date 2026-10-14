using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.PromptRequests;

public record GetPromptRequestsByQuestionBankQuery(Guid TrainingId, Guid QuestionBankId)
    : IRequest<Result<IReadOnlyList<PromptRequestDTO>>>;
