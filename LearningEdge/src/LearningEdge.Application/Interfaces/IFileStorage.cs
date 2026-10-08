namespace LearningEdge.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>Stores the content and returns the key needed to read or delete it later.</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
