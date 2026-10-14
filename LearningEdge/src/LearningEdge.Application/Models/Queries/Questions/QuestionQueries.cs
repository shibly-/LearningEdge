using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Questions;

public record GetQuestionsByQuestionBankQuery(Guid TrainingId, Guid QuestionBankId, bool ActiveOnly)
    : IRequest<Result<IReadOnlyList<QuestionDTO>>>;

public record GetQuestionByIdQuery(Guid TrainingId, Guid QuestionBankId, Guid Id)
    : IRequest<Result<QuestionDTO>>;
