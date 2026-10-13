using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

[Table("McqQuestion", Schema = "le")]
public class McqQuestion(string text, Guid examId, int score, IEnumerable<Option> options, SingleChoiceAnswer answer) : Question(text, examId, score)
{
    public IReadOnlyCollection<Option> Options { get; private set; } = options.ToList().AsReadOnly();
    public SingleChoiceAnswer Answer { get; private set; } = answer;
}

