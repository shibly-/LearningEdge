using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.ValueObjects;

public class TrueFalseAnswer : Answer
{
    public bool CorrectValue { get; private set; }

    public TrueFalseAnswer(bool correctValue)
    {
        CorrectValue = correctValue;
    }

    public override bool IsCorrect(object response)
    {
        return response is bool value && value == CorrectValue;
    }
}
