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
    bool IsFileExtracted,
    bool IsMCQGenerated,
    bool IsFileExtractionStarted,
    bool IsMCQGenerationStarted,
    bool IsFileExtractionFailed,
    bool IsMCQGenerationFailed,
    string FileExtractionFailureMessage,
    string MCQGenerationFailureMessage,
    DateTime CreatedAt
);