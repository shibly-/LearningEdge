namespace LearningEdge.Application.Models.DTOs;

public record TrainingDTO(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Description,
    bool IsActive,
    IReadOnlyList<TrainingFileDTO> Files
);

public record TrainingFileDTO(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    DateTime CreatedAt
);
