using System.Globalization;
using LearningEdge.Application.Caching;
using LearningEdge.Application.Interfaces;

namespace LearningEdge.Api.Hubs;

public sealed class FileExtractProgressReader
{
    private readonly ICacheService _cache;

    public FileExtractProgressReader(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<FileExtractProgressUpdate> ReadAsync(Guid fileId)
    {
        var status = await _cache.GetAsync(FileExtractCache.StatusValueKey(fileId)) ?? string.Empty;
        var progress = status is FileExtractCache.Status.Queued or ""
            ? 0
            : ReadCeilingPercent(await _cache.GetAsync(FileExtractCache.ProgressKey(fileId)));

        if (status == FileExtractCache.Status.Completed)
        {
            progress = 100;
        }

        return new FileExtractProgressUpdate(fileId, status, progress);
    }

    /// <summary>
    /// The document parser stores the percentage already passed through Math.Ceiling.
    /// Ceiling again so a fractional value still displays as a whole percent.
    /// </summary>
    public static int ReadCeilingPercent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }

        return Math.Clamp((int)Math.Ceiling(value), 0, 100);
    }
}
