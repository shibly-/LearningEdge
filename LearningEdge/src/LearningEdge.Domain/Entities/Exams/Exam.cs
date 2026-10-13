using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Questions;

namespace LearningEdge.Domain.Entities.Exams;

public sealed class Exam : BaseEntity<Guid>
{
    public string Title { get; private set; }
    public Guid TrainingId { get; private set; }
    public Guid PreparedByUserId { get; private set; }

    private readonly List<Question> _questions = new();
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public Exam(string title, Guid trainingId, Guid preparedByUserId)
    {
        Id = Guid.NewGuid();
        Title = title;
        TrainingId = trainingId;
        PreparedByUserId = preparedByUserId;
    }

    public void AddQuestion(Question question) => _questions.Add(question);

    public Question? RemoveQuestion(Guid questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            _questions.Remove(question);
        }
        return question;
    }
}

