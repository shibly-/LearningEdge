using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
    public void SoftDelete()
    {
        IsDeleted = true;
        MarkUpdated();
    }
}
