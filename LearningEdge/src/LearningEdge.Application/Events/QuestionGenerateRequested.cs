using LearningEdge.Messaging.Contracts.Events;

namespace LearningEdge.Application.Events;

public record QuestionGenerationSubmitted(
    Guid PromptRequestId,
    Guid TrainingId,
    Guid QuestionBankId,
    string PromptText,
    string QuestionType,
    int QuestionCount,
    DateTime RequestedOn,
    string Status
) : IQuestionGenerationSubmitted;
