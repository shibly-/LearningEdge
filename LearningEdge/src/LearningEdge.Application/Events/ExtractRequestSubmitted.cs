using LearningEdge.Messaging.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Application.Events;

public record ExtractRequestSubmitted(
    Guid FileId,
    string? FileName,
    string? ContentType,
    long SizeBytes,
    DateTime CreatedAt,
    Guid UploadedByUserId,
    Guid RequestedByUserId,
    DateTime RequestedAt
) : IExtractRequestSubmitted;
