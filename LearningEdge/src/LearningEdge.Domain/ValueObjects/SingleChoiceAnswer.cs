namespace LearningEdge.Domain.ValueObjects;

public class SingleChoiceAnswer : Answer
{
    public Option CorrectOption { get; private set; }

    public SingleChoiceAnswer(Option correctOption)
    {
        CorrectOption = correctOption ?? throw new ArgumentNullException(nameof(correctOption));
    }

    public override bool IsCorrect(object response)
    {
        return response is Option option && option.Text == CorrectOption.Text;
    }
}

