namespace LearningEdge.Domain.ValueObjects;

public class PromptRequest
{
    public string PromptText { get; private set; }
    public QuestionType QuestionType { get; private set; }
    public int QuestionCount { get; private set; }

    public PromptRequest(string promptText, QuestionType questionType, int questionCount)
    {
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("Prompt text cannot be empty.");
        }

        if (questionCount <= 0)
        {
            throw new ArgumentException("Question count must be positive.");
        }

        PromptText = promptText;
        QuestionType = questionType;
        QuestionCount = questionCount;
    }
}

