using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Trainings;

public record CreateTrainingCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    bool IsActive = true
) : IRequest<Result<Guid>>;

public record UpdateTrainingCommand(
    Guid CategoryId,
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
) : IRequest<Result<TrainingDTO>>;

/// <summary>A file to upload. The caller owns <see cref="Content"/> and disposes it after the command completes.</summary>
public record TrainingFileUpload(string FileName, string ContentType, long Length, Stream Content);

public record UploadTrainingFilesCommand(
    Guid CategoryId,
    Guid TrainingId,
    Guid UploadedByUserId,
    IReadOnlyList<TrainingFileUpload> Files
) : IRequest<Result<IReadOnlyList<TrainingFileDTO>>>;

public record RemoveTrainingFileCommand(
    Guid CategoryId,
    Guid TrainingId,
    Guid FileId,
    Guid RemovedByUserId
) : IRequest<Result<Guid>>;
