using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

public class McqQuestion : Question
{
    public IReadOnlyCollection<Option> Options { get; private set; }
    public SingleChoiceAnswer Answer { get; private set; }

    public McqQuestion(string text, Guid examId, int score, IEnumerable<Option> options, SingleChoiceAnswer answer)
        : base(text, examId, score)
    {
        Options = options.ToList().AsReadOnly();
        Answer = answer;
    }
}

