using LearningEdge.Domain.Common;
using LearningEdge.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningEdge.Domain.Entities.Trainings;

[Table("PromptRequest", Schema = "le")]
public class PromptRequest : BaseEntity<Guid>
{    
    public Guid TrainingId { get; private set; }
    public string PromptText { get; private set; }
    public QuestionType QuestionType { get; private set; }
    public int QuestionCount { get; private set; }
    public DateTime RequestedOn { get; private set; }

    public PromptStatus Status { get; private set; }

    public PromptRequest(Guid trainingId, string promptText, QuestionType questionType, int questionCount)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("Prompt text cannot be empty.");
        }

        if (questionCount <= 0)
        {
            throw new ArgumentException("Question count must be positive.");
        }

        Id = Guid.NewGuid();
        TrainingId = trainingId;
        PromptText = promptText;
        QuestionType = questionType;
        QuestionCount = questionCount;
        RequestedOn = DateTime.UtcNow;
        Status = PromptStatus.Pending;
    }

    public void MarkCompleted() => Status = PromptStatus.Completed;
    public void MarkFailed() => Status = PromptStatus.Failed;
}

public enum PromptStatus
{
    Pending,
    Completed,
    Failed
}

