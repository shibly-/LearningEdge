using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Domain.Entities.Questions;

[System.ComponentModel.DataAnnotations.Schema.Table("MultipleSelectionQuestion", Schema = "le")]
public class MultipleSelectionQuestion : Question
{
    public IReadOnlyCollection<Option> Options { get; private set; } = [];
    public MultipleChoiceAnswer Answer { get; private set; } = new([new Option("-")]);

    private MultipleSelectionQuestion()
    {
    }

    public MultipleSelectionQuestion(
        string text,
        Guid questionBankId,
        int score,
        IEnumerable<Option> options,
        MultipleChoiceAnswer answer,
        bool isActive = true)
        : base(text, questionBankId, score, isActive)
    {
        Options = CopyOptions(options);
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
    }

    public void Update(string text, int score, bool isActive, IEnumerable<Option> options, MultipleChoiceAnswer answer)
    {
        ReplaceContent(text, score, isActive);
        Options = CopyOptions(options);
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
    }

    private static IReadOnlyCollection<Option> CopyOptions(IEnumerable<Option> options)
    {
        var copy = options?.ToList() ?? throw new ArgumentNullException(nameof(options));
        if (copy.Count == 0)
        {
            throw new ArgumentException("At least one option is required.");
        }

        return copy.AsReadOnly();
    }
}
