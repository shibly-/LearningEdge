using LearningEdge.Domain.Common;

namespace LearningEdge.Domain.Entities.Questions;

public abstract class Question : BaseEntity<Guid>
{
    public string Text { get; protected set; }
    public Guid QuizLibraryId { get; protected set; }
    public int Score { get; protected set; }

    protected Question(string text, Guid quizLibraryId, int score)
    {
        Id = Guid.NewGuid();
        Text = text;
        QuizLibraryId = quizLibraryId;
        Score = score > 0 ? score : throw new ArgumentException("Score must be positive.");
    }
}
