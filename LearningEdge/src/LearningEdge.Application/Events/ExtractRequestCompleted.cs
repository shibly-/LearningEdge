using LearningEdge.Messaging.Contracts.Events;

namespace LearningEdge.Application.Events;

public record ExtractRequestCompleted(
    Guid FileId,
    Guid TrainingId,
    bool IsSuccessful,
    string? FailedReason
) : IExtractRequestCompleted;
