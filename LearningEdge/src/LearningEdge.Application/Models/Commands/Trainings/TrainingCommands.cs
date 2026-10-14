using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Messaging.Contracts.Events;
using MediatR;

namespace LearningEdge.Application.Models.Commands.Trainings;

public record CreateTrainingCommand(
    Guid OrganizationId,
    Guid CategoryId,
    string Name,
    string? Description,
    bool IsActive = true
) : IRequest<Result<Guid>>;

public record UpdateTrainingCommand(
    Guid OrganizationId,
    Guid CategoryId,
    Guid Id,
    string Name,
    string? Description,
    bool IsActive
) : IRequest<Result<TrainingDTO>>;

/// <summary>A file to upload. The caller owns <see cref="Content"/> and disposes it after the command completes.</summary>
public record TrainingFileUpload(string FileName, string ContentType, long Length, Stream Content);

public record UploadTrainingFilesCommand(
    Guid OrganizationId,
    Guid CategoryId,
    Guid TrainingId,
    Guid UploadedByUserId,
    IReadOnlyList<TrainingFileUpload> Files
) : IRequest<Result<IReadOnlyList<TrainingFileDTO>>>;

public record RemoveTrainingFileCommand(
    Guid OrganizationId,
    Guid CategoryId,
    Guid TrainingId,
    Guid FileId,
    Guid RemovedByUserId
) : IRequest<Result<Guid>>;

public record UpdateTrainingInitiateFileExtractCommand(
    Guid OrganizationId,
    Guid CategoryId,
    Guid TrainingId,
    Guid FileId,
    Guid RequestedByUserId
) : IRequest<Result<IReadOnlyList<TrainingFileDTO>>>;

public record UpdateTrainingFileExtractCompletedCommand(    
    Guid FileId,
    Guid TrainingId,
    bool IsSuccessful,
    string? FailedReason
) : IRequest<Result<bool>>;
