using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Infrastructure.Constants;

public static class DbContextDefaultOptions
{
    public const int MaxRetryCount = 10;
    public const int MaxRetryDelay = 2;
    public const string errorNumbersToAdd = null;
}
