using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.ValueObjects;

public class MultipleChoiceAnswer : Answer
{
    public IReadOnlyCollection<Option> CorrectOptions { get; private set; }

    public MultipleChoiceAnswer(IEnumerable<Option> correctOptions)
    {
        var list = correctOptions?.ToList() ?? throw new ArgumentNullException(nameof(correctOptions));
        if (!list.Any())
        {
            throw new ArgumentException("At least one correct option must be provided.");
        }

        CorrectOptions = list.AsReadOnly();
    }

    public override bool IsCorrect(object response)
    {
        if (response is IEnumerable<Option> selectedOptions)
        {
            return selectedOptions.All(o => CorrectOptions.Any(c => c.Text == o.Text)) &&
                   CorrectOptions.All(c => selectedOptions.Any(o => o.Text == c.Text));
        }
        return false;
    }
}

