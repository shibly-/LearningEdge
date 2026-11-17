using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Queries.Platform;

public record GetPlatformSnapshotQuery : IRequest<Result<PlatformSnapshotDTO>>;
