using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Messaging.Contracts.Events;

public interface IExtractRequestCompleted
{
    Guid FileId { get; }
    Guid TrainingId { get; }
    bool IsSuccessful { get; }
    string? FailedReason { get; } 
}

