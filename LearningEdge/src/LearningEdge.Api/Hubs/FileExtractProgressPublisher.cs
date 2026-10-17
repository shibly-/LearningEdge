using Microsoft.AspNetCore.SignalR;

namespace LearningEdge.Api.Hubs;

public sealed class FileExtractProgressPublisher : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    private readonly FileExtractWatchRegistry _registry;
    private readonly FileExtractProgressReader _reader;
    private readonly IHubContext<FileExtractProgressHub> _hub;
    private readonly ILogger<FileExtractProgressPublisher> _logger;
    private readonly Dictionary<Guid, string> _lastSent = new();

    public FileExtractProgressPublisher(
        FileExtractWatchRegistry registry,
        FileExtractProgressReader reader,
        IHubContext<FileExtractProgressHub> hub,
        ILogger<FileExtractProgressPublisher> logger)
    {
        _registry = registry;
        _reader = reader;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read file extract progress from Redis.");
            }
        }
    }

    private async Task PublishAsync(CancellationToken cancellationToken)
    {
        var active = _registry.ActiveFileIds();
        if (active.Count == 0)
        {
            _lastSent.Clear();
            return;
        }

        var activeSet = active as IReadOnlySet<Guid> ?? active.ToHashSet();
        foreach (var stale in _lastSent.Keys.Where(id => !activeSet.Contains(id)).ToArray())
        {
            _lastSent.Remove(stale);
        }

        foreach (var fileId in active)
        {
            var update = await _reader.ReadAsync(fileId);
            var token = $"{update.Status}:{update.Progress}";
            if (_lastSent.TryGetValue(fileId, out var previous) && previous == token)
            {
                continue;
            }

            _lastSent[fileId] = token;
            await _hub.Clients
                .Group(FileExtractProgressHub.GroupName(fileId))
                .SendAsync("progress", update, cancellationToken);
        }
    }
}
