using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

public class MultipleSelectionQuestion : Question
{
    public IReadOnlyCollection<Option> Options { get; private set; }
    public MultipleChoiceAnswer Answer { get; private set; }

    public MultipleSelectionQuestion(string text, Guid examId, int score, IEnumerable<Option> options, MultipleChoiceAnswer answer)
        : base(text, examId, score)
    {
        Options = options.ToList().AsReadOnly();
        Answer = answer;
    }
}

