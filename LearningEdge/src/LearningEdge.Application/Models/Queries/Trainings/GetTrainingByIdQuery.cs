using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Trainings;

public record GetTrainingByIdQuery(Guid CategoryId, Guid Id) : IRequest<Result<TrainingDTO>>;

public record GetTrainingsQuery(Guid CategoryId) : IRequest<Result<IReadOnlyList<TrainingDTO>>>;
