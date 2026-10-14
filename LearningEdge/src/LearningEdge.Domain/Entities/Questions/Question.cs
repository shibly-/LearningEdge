using System.ComponentModel.DataAnnotations.Schema;
using LearningEdge.Domain.Common;

namespace LearningEdge.Domain.Entities.Questions;

[Table("Question", Schema = "le")]
public abstract class Question : BaseEntity<Guid>
{
    public string Text { get; protected set; } = string.Empty;
    public Guid QuestionBankId { get; protected set; }
    public int Score { get; protected set; }
    public bool IsActive { get; protected set; }

    /// <summary>Used by EF Core when materializing a stored question.</summary>
    protected Question()
    {
    }

    protected Question(string text, Guid questionBankId, int score, bool isActive = true)
    {
        Id = Guid.NewGuid();
        Text = RequireText(text);
        QuestionBankId = questionBankId;
        Score = RequireScore(score);
        IsActive = isActive;
    }

    protected void ReplaceContent(string text, int score, bool isActive)
    {
        Text = RequireText(text);
        Score = RequireScore(score);
        IsActive = isActive;
        MarkUpdated();
    }

    private static string RequireText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Question text cannot be empty.");
        }

        return text.Trim();
    }

    private static int RequireScore(int score)
    {
        if (score <= 0)
        {
            throw new ArgumentException("Score must be positive.");
        }

        return score;
    }
}
