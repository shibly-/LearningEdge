namespace LearningEdge.Messaging.Contracts.Events;

/// <summary>
/// Published to the <c>question-generate</c> RabbitMQ exchange when a prompt request is stored.
/// </summary>
public interface IQuestionGenerationSubmitted
{
    Guid PromptRequestId { get; }
    Guid TrainingId { get; }
    Guid QuestionBankId { get; }
    string PromptText { get; }
    string QuestionType { get; }
    int QuestionCount { get; }
    DateTime RequestedOn { get; }
    string Status { get; }
}
