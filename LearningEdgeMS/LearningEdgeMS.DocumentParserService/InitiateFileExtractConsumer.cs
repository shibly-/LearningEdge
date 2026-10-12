using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
//using Microsoft.KernelMemory;

namespace LearningEdgeMS.DocumentParserService;

public sealed class InitiateFileExtractConsumer : IConsumer<IExtractRequestSubmitted>
{
    //private readonly IKernelMemory _memory;
    private readonly DocumentParserSettings _settings;
    private readonly ILogger<InitiateFileExtractConsumer> _logger;

    public InitiateFileExtractConsumer(
        //IKernelMemory memory,
        DocumentParserSettings settings,
        ILogger<InitiateFileExtractConsumer> logger)
    {
        //_memory = memory;
        _settings = settings;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IExtractRequestSubmitted> context)
    {
        var message = context.Message;
        var filePath = ResolveStoredFile(message);
        var fileName = string.IsNullOrWhiteSpace(message.FileName)
            ? Path.GetFileName(filePath)
            : message.FileName;

        Console.WriteLine($"Received extract request for file {fileName} ({message.FileId}) uploaded by {message.UploadedByUserId}");

        _logger.LogInformation(
            "Starting extraction for {FileName} ({FileId})",
            fileName,
            message.FileId);

        /*
        await using var content = File.OpenRead(filePath);
        var tags = new TagCollection();
        tags.Add("fileId", message.FileId.ToString());
        tags.Add("fileName", fileName);
        tags.Add("uploadedBy", message.UploadedByUserId.ToString());

        await _memory.ImportDocumentAsync(
            content,
            fileName,
            documentId: message.FileId.ToString(),
            tags: tags,
            index: _settings.Index,
            cancellationToken: context.CancellationToken);

        */

        _logger.LogInformation(
            "Stored embeddings for {FileName} ({FileId}) in index {Index}",
            fileName,
            message.FileId,
            _settings.Index);
    }

    private string ResolveStoredFile(IExtractRequestSubmitted message)
    {
        if (string.IsNullOrWhiteSpace(message.StorageKey))
        {
            throw new InvalidOperationException(
                $"Extract request {message.FileId} did not include a storage key.");
        }

        var root = Path.GetFullPath(_settings.StorageRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, message.StorageKey));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Storage key '{message.StorageKey}' resolves outside the storage root.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Uploaded file '{message.StorageKey}' was not found.",
                fullPath);
        }

        return fullPath;
    }
}

public sealed record DocumentParserSettings(string StorageRootPath, string Index);
