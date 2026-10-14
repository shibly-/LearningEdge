using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.QuestionBanks;

public record GetQuestionBanksByTrainingQuery(Guid TrainingId) : IRequest<Result<IReadOnlyList<QuestionBankDTO>>>;

public record GetQuestionBankByIdQuery(Guid TrainingId, Guid Id) : IRequest<Result<QuestionBankDTO>>;

public record GetActiveQuestionBanksQuery : IRequest<Result<IReadOnlyList<QuestionBankDTO>>>;
