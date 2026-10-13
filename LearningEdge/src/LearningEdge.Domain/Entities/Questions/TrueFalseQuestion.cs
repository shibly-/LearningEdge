using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

public class TrueFalseQuestion : Question
{
    public TrueFalseAnswer Answer { get; private set; }

    public TrueFalseQuestion(string text, Guid examId, int score, TrueFalseAnswer answer)
        : base(text, examId, score)
    {
        Answer = answer;
    }
}

