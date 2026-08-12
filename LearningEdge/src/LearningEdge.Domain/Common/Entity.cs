using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
