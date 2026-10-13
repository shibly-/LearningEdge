namespace LearningEdge.Domain.ValueObjects;

public class Option
{
    public string Text { get; private set; }

    public Option(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Option text cannot be empty.");
        }

        Text = text;
    }

    public override string ToString() => Text;
}
