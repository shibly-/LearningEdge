using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

[Table("MultipleSelectionQuestion", Schema = "le")]
public class MultipleSelectionQuestion(string text, Guid examId, int score, IEnumerable<Option> options, MultipleChoiceAnswer answer) : Question(text, examId, score)
{
    public IReadOnlyCollection<Option> Options { get; private set; } = options.ToList().AsReadOnly();
    public MultipleChoiceAnswer Answer { get; private set; } = answer;
}

