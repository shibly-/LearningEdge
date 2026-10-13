namespace LearningEdge.Domain.ValueObjects;

public class SingleChoiceAnswer(Option correctOption) : Answer
{
    public Option CorrectOption { get; private set; } = correctOption ?? throw new ArgumentNullException(nameof(correctOption));

    public override bool IsCorrect(object response)
    {
        return response is Option option && option.Text == CorrectOption.Text;
    }
}

