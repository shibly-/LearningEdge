using LearningEdge.Messaging.Contracts.Events;

namespace LearningEdge.Application.Events;

public record QuestionGenerateRequested(
    Guid PromptRequestId,
    Guid TrainingId,
    Guid QuestionBankId,
    string PromptText,
    string QuestionType,
    int QuestionCount,
    DateTime RequestedOn,
    string Status
) : IQuestionGenerateRequested;
