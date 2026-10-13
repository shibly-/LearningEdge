using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.ValueObjects;

public abstract class Answer
{
    public abstract bool IsCorrect(object response);
}
