using LearningEdge.Application.Interfaces;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
//using Microsoft.KernelMemory;

namespace LearningEdgeMS.DocumentParserService.EventConsumers;

public sealed class InitiateFileExtractConsumer : IConsumer<IExtractRequestSubmitted>
{
    private readonly IApplicationDbContext _appDbContext;
    //private readonly IKernelMemory _memory;
    private readonly DocumentParserSettings _settings;
    private readonly ILogger<InitiateFileExtractConsumer> _logger;    

    public InitiateFileExtractConsumer(
        IApplicationDbContext appDbContext,
        //IKernelMemory memory,
        DocumentParserSettings settings,
        ILogger<InitiateFileExtractConsumer> logger)
    {
        _appDbContext = appDbContext;
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

        await MarkFileExtractionStartedAsync(message.FileId, context.CancellationToken);

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

    private async Task MarkFileExtractionStartedAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var updated = await _appDbContext.TrainingFiles
            .Where(file => file.Id == fileId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(file => file.IsFileExtractionStarted, true),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"No training file found with Id {fileId}.");
        }
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
