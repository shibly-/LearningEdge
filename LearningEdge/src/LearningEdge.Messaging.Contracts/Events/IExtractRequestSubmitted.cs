using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Messaging.Contracts.Events;

public interface IExtractRequestSubmitted
{
    Guid FileId { get; }
    string? FileName { get; }
    string? ContentType { get; }
    long SizeBytes { get; }
    DateTime CreatedAt { get; }
    Guid UploadedByUserId { get; }
    Guid RequestedByUserId { get; }
    DateTime RequestedAt { get; }
}
