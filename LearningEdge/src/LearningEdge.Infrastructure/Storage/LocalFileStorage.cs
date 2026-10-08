using LearningEdge.Application.Interfaces;

namespace LearningEdge.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken)
    {
        // The stored name is generated; the client's file name never reaches the file system.
        var storageKey = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}";
        var fullPath = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, cancellationToken);

        return storageKey;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Storage key '{storageKey}' resolves outside the storage root.");
        }

        return fullPath;
    }
}
