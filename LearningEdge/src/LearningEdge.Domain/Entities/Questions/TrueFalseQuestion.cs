using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Domain.Entities.Questions;

[System.ComponentModel.DataAnnotations.Schema.Table("TrueFalseQuestion", Schema = "le")]
public class TrueFalseQuestion : Question
{
    public TrueFalseAnswer Answer { get; private set; } = new(false);

    private TrueFalseQuestion()
    {
    }

    public TrueFalseQuestion(string text, Guid questionBankId, int score, TrueFalseAnswer answer, bool isActive = true)
        : base(text, questionBankId, score, isActive)
    {
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
    }

    public void Update(string text, int score, bool isActive, TrueFalseAnswer answer)
    {
        ReplaceContent(text, score, isActive);
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
    }
}
