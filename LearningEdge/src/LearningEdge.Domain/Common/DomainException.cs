using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Common;

public sealed class DomainException(string message) : Exception(message);
