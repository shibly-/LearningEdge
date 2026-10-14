using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Exams;

public record GetExamsByTrainingQuery(Guid TrainingId) : IRequest<Result<IReadOnlyList<ExamDTO>>>;

public record GetExamByIdQuery(Guid TrainingId, Guid Id) : IRequest<Result<ExamDTO>>;

public record GetExamQuestionsQuery(Guid TrainingId, Guid ExamId) : IRequest<Result<IReadOnlyList<QuestionDTO>>>;
