using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LearningEdge.Domain.Entities.Questions;

[Table("TrueFalseQuestion", Schema = "le")]
public class TrueFalseQuestion : Question
{
    public TrueFalseAnswer Answer { get; private set; }

    public TrueFalseQuestion(string text, Guid examId, int score, TrueFalseAnswer answer)
        : base(text, examId, score)
    {
        Answer = answer;
    }
}

