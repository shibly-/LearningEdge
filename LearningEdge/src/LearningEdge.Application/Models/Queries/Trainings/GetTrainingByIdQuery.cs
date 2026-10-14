using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Trainings;

public record GetTrainingByIdQuery(Guid OrganizationId, Guid CategoryId, Guid Id) : IRequest<Result<TrainingDTO>>;

public record GetTrainingsQuery(Guid OrganizationId, Guid CategoryId) : IRequest<Result<IReadOnlyList<TrainingDTO>>>;
