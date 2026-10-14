using System.ComponentModel.DataAnnotations.Schema;
using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Questions;

namespace LearningEdge.Domain.Entities.Exams;

[Table("Exam", Schema = "le")]
public sealed class Exam : BaseEntity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public Guid TrainingId { get; private set; }
    public Guid PreparedByUserId { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Question> _questions = [];
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    private Exam()
    {
    }

    public Exam(string title, Guid trainingId, Guid preparedByUserId, bool isActive = true)
    {
        Id = Guid.NewGuid();
        Title = RequireTitle(title);
        TrainingId = trainingId;
        PreparedByUserId = preparedByUserId;
        IsActive = isActive;
    }

    public void Update(string title, bool isActive)
    {
        Title = RequireTitle(title);
        IsActive = isActive;
        MarkUpdated();
    }

    public void AddQuestion(Question question) => _questions.Add(question);

    public Question? RemoveQuestion(Guid questionId)
    {
        var question = _questions.FirstOrDefault(candidate => candidate.Id == questionId);
        if (question is not null)
        {
            _questions.Remove(question);
        }

        return question;
    }

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.");
        }

        return title.Trim();
    }
}
