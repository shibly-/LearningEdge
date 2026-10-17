namespace LearningEdge.Api.Hubs;

public sealed class FileExtractWatchRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<Guid>> _byConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, int> _counts = new();

    public void Watch(string connectionId, Guid fileId)
    {
        lock (_gate)
        {
            if (!_byConnection.TryGetValue(connectionId, out var files))
            {
                files = [];
                _byConnection[connectionId] = files;
            }

            if (files.Add(fileId))
            {
                _counts[fileId] = _counts.GetValueOrDefault(fileId) + 1;
            }
        }
    }

    public void Unwatch(string connectionId, Guid fileId)
    {
        lock (_gate)
        {
            Release(connectionId, fileId);
        }
    }

    public void ReleaseConnection(string connectionId)
    {
        lock (_gate)
        {
            if (!_byConnection.TryGetValue(connectionId, out var files))
            {
                return;
            }

            foreach (var fileId in files.ToArray())
            {
                Release(connectionId, fileId);
            }
        }
    }

    public IReadOnlyCollection<Guid> ActiveFileIds()
    {
        lock (_gate)
        {
            return _counts.Keys.ToArray();
        }
    }

    private void Release(string connectionId, Guid fileId)
    {
        if (!_byConnection.TryGetValue(connectionId, out var files) || !files.Remove(fileId))
        {
            return;
        }

        if (files.Count == 0)
        {
            _byConnection.Remove(connectionId);
        }

        var remaining = _counts.GetValueOrDefault(fileId) - 1;
        if (remaining <= 0)
        {
            _counts.Remove(fileId);
        }
        else
        {
            _counts[fileId] = remaining;
        }
    }
}
