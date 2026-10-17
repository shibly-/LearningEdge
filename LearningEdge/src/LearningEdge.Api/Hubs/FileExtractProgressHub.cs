using Microsoft.AspNetCore.SignalR;

namespace LearningEdge.Api.Hubs;

public sealed class FileExtractProgressHub : Hub
{
    private readonly FileExtractWatchRegistry _registry;
    private readonly FileExtractProgressReader _reader;

    public FileExtractProgressHub(FileExtractWatchRegistry registry, FileExtractProgressReader reader)
    {
        _registry = registry;
        _reader = reader;
    }

    public static string GroupName(Guid fileId) => $"file-extract:{fileId:D}";

    public async Task Watch(Guid fileId)
    {
        _registry.Watch(Context.ConnectionId, fileId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(fileId));
        var snapshot = await _reader.ReadAsync(fileId);
        await Clients.Caller.SendAsync("progress", snapshot);
    }

    public async Task Unwatch(Guid fileId)
    {
        _registry.Unwatch(Context.ConnectionId, fileId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(fileId));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.ReleaseConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
