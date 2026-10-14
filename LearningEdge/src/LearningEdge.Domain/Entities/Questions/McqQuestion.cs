using LearningEdge.Domain.ValueObjects;

namespace LearningEdge.Domain.Entities.Questions;

[System.ComponentModel.DataAnnotations.Schema.Table("McqQuestion", Schema = "le")]
public class McqQuestion : Question
{
    public IReadOnlyCollection<Option> Options { get; private set; } = [];
    public SingleChoiceAnswer Answer { get; private set; } = new(new Option("-"));

    private McqQuestion()
    {
    }

    public McqQuestion(
        string text,
        Guid questionBankId,
        int score,
        IEnumerable<Option> options,
        SingleChoiceAnswer answer,
        bool isActive = true)
        : base(text, questionBankId, score, isActive)
    {
        Options = CopyOptions(options);
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
    }

    public void Update(string text, int score, bool isActive, IEnumerable<Option> options, SingleChoiceAnswer answer)
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
