using LearningEdge.Domain.Common;

namespace LearningEdge.Domain.Entities.Questions;

public abstract class Question : BaseEntity<Guid>
{
    public string Text { get; protected set; }
    public Guid ExamId { get; protected set; }
    public int Score { get; protected set; }

    protected Question(string text, Guid examId, int score)
    {
        Id = Guid.NewGuid();
        Text = text;
        ExamId = examId;
        Score = score > 0 ? score : throw new ArgumentException("Score must be positive.");
    }
}
