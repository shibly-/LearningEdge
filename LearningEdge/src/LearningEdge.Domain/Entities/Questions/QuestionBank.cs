using LearningEdge.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

[Table("QuestionBank", Schema = "le")]
public class QuestionBank : BaseEntity<Guid>
{
    public string Title { get; private set; }
    public Guid TrainingId { get; private set; }

    private readonly List<Question> _questions = [];
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public QuestionBank(string title, Guid trainingId)
    {
        Id = Guid.NewGuid();
        Title = title;
        TrainingId = trainingId;
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
