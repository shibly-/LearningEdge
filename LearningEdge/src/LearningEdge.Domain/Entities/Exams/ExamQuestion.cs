using System.ComponentModel.DataAnnotations.Schema;

namespace LearningEdge.Domain.Entities.Exams;

[Table("ExamQuestion", Schema = "le")]
public class ExamQuestion(Guid examId, Guid questionId)
{
    public Guid ExamId { get; private set; } = examId;
    public Guid QuestionId { get; private set; } = questionId;
}