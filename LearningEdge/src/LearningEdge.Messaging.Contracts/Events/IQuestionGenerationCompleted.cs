using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Messaging.Contracts.Events;

public interface IQuestionGenerationCompleted
{
    Guid PromptRequestId { get; }
    Guid TrainingId { get; }
    Guid QuestionBankId { get; }
    bool IsSuccessful { get; }
    string? FailedReason { get; }
}