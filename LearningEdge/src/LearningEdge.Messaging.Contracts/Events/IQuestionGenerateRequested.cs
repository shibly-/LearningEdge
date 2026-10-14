namespace LearningEdge.Messaging.Contracts.Events;

/// <summary>
/// Published to the <c>question-generate</c> RabbitMQ exchange when a prompt request is stored.
/// </summary>
public interface IQuestionGenerateRequested
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

public static class MessagingEntityNames
{
    public const string QuestionGenerate = "question-generate";
}
