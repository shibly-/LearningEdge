using LearningEdge.Application.Caching;
using LearningEdge.Application.Events;
using LearningEdge.Application.Interfaces;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Messaging.Contracts.Events;
using LearningEdgeMS.DocumentParserService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Diagnostics;

namespace LearningEdgeMS.DocumentParserService.Messaging.Consumers;

public sealed class FileExtractRequestConsumer : IConsumer<IExtractRequestSubmitted>
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _appDbContext;
    private readonly DocumentParserSettings _settings;
    private readonly QdrantClient _qdrantClient;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<FileExtractRequestConsumer> _logger;
    private readonly ICacheService _cache;
    private readonly string _collectionName;

    public FileExtractRequestConsumer(
        IConfiguration configuration,
        ApplicationDbContext appDbContext,
        DocumentParserSettings settings,
        EmbeddingService embeddingService,
        QdrantClient qdrantClient,
        ICacheService cache,
        ILogger<FileExtractRequestConsumer> logger)
    {
        _configuration = configuration;
        _collectionName = _configuration["VectorDB:Qdrant:CollectionName"] ?? "le_docs";
        _appDbContext = appDbContext;
        _settings = settings;
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the IExtractRequestSubmitted event and initiates the file extraction process.
    /// </summary>
    public async Task Consume(ConsumeContext<IExtractRequestSubmitted> context)
    {        
        _logger.LogInformation("Using Embdedding Model: {Model}...", _configuration["OLLama:Model"]);
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var fileStatusInCache = await _cache.GetAsync(FileExtractCache.StatusValueKey(message.FileId));
        if (fileStatusInCache == FileExtractCache.Status.Completed)
        {
            // Cache already says finished. Publish again so the API records it if the
            // previous completion message was lost, and do not embed a second time.
            _logger.LogInformation(
                "File extraction already completed for FileId: {FileId}. Republishing completion.",
                message.FileId);

            var existing = await FindTrainingFileAsync(message.FileId, cancellationToken);
            if (existing is null)
            {
                _logger.LogWarning(
                    "Completed extract for {FileId} has no training file row. Skipping republish.",
                    message.FileId);
                return;
            }

            await PublishThenSetStatusAsync(
                context,
                new ExtractRequestCompleted(message.FileId, existing.TrainingId, true, null),
                FileExtractCache.Status.Completed);
            return;
        }

        _logger.LogInformation(
            "Starting file extraction for FileId: {FileId} with status: {Status}",
            message.FileId,
            fileStatusInCache);

        await _cache.SetAsync(FileExtractCache.ProgressKey(message.FileId), "0", FileExtractCache.StatusLifetime);
        await SetExtractStatusAsync(message.FileId, FileExtractCache.Status.Processing);

        IExtractRequestCompleted completed;
        try
        {
            completed = await IngestAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var trainingId = ex is FileExtractFailedException failed
                ? failed.TrainingId
                : await TryGetTrainingIdAsync(message.FileId);

            if (ex is FileExtractFailedException or FileNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "File extraction failed for {FileId}", message.FileId);
            }
            else
            {
                _logger.LogError(ex, "An error occurred while processing the document ingestion pipeline for {FileId}", message.FileId);
            }

            // Publish first. The cache is marked Failed only after the API can observe the result.
            // If publish throws, the message is redelivered and this attempt runs again.
            await PublishThenSetStatusAsync(
                context,
                new ExtractRequestCompleted(message.FileId, trainingId, false, ex.Message),
                FileExtractCache.Status.Failed);
            return;
        }

        // Outside the catch: a publish failure must redeliver the original request,
        // not record the extract as failed after the vectors were stored.
        await PublishThenSetStatusAsync(context, completed, FileExtractCache.Status.Completed);
    }

    private async Task<IExtractRequestCompleted> IngestAsync(
        IExtractRequestSubmitted message,
        CancellationToken cancellationToken)
    {
        var filePath = ResolveStoredFile(message);
        var fileName = string.IsNullOrWhiteSpace(message.FileName)
            ? Path.GetFileName(filePath)
            : message.FileName;

        _logger.LogInformation(
            "Received extract request for file {FileName} ({FileId}) uploaded by {UploadedByUserId}",
            fileName,
            message.FileId,
            message.UploadedByUserId);

        var trainingFile = await FindTrainingFileAsync(message.FileId, cancellationToken);
        if (trainingFile is null)
        {
            throw new FileExtractFailedException(Guid.Empty, $"Training file with ID {message.FileId} was not found.");
        }

        var training = await _appDbContext.Trainings
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == trainingFile.TrainingId, cancellationToken);

        if (training is null)
        {
            throw new FileExtractFailedException(
                trainingFile.TrainingId,
                $"Training with ID {trainingFile.TrainingId} was not found.");
        }

        await MarkFileExtractionStartedAsync(message.FileId, trainingFile.TrainingId, cancellationToken);

        _logger.LogInformation("Parsing file: {FilePath}", filePath);
        var parseWatch = Stopwatch.StartNew();
        string rawText = await DocumentParsingService.ExtractTextAsync(filePath, cancellationToken);
        parseWatch.Stop();
        _logger.LogInformation("Parsing elapsed {ElapsedMilliseconds} ms", parseWatch.ElapsedMilliseconds);

        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new FileExtractFailedException(trainingFile.TrainingId, "No text extracted from file.");
        }

        _logger.LogInformation("Generating chunks and embedding vectors for {FileId}", message.FileId);
        var embedWatch = Stopwatch.StartNew();
        var points = await _embeddingService.ProcessAndEmbedTextAsync(message.FileId, rawText, cancellationToken);
        embedWatch.Stop();
        _logger.LogInformation(
            "Embedding elapsed {ElapsedMilliseconds} ms for {Count} chunks",
            embedWatch.ElapsedMilliseconds,
            points.Count);

        if (points.Count == 0)
        {
            throw new FileExtractFailedException(trainingFile.TrainingId, "No text chunks were produced from the file.");
        }

        var collectionName = _collectionName + "_" + training.OrganizationId.ToString("N");
        _logger.LogInformation(
            "Qdrant collection {Collection} will store {Count} chunks for {FileId}",
            collectionName,
            points.Count,
            message.FileId);

        await EnsureVectorDBCollectionExistsAsync(collectionName, cancellationToken);
        await DeleteFilePointsAsync(collectionName, message.FileId, cancellationToken);
        await _qdrantClient.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Stored {Count} chunks in Qdrant collection {Collection}",
            points.Count,
            collectionName);

        return new ExtractRequestCompleted(message.FileId, trainingFile.TrainingId, true, null);
    }

    private async Task PublishThenSetStatusAsync(
        ConsumeContext<IExtractRequestSubmitted> context,
        IExtractRequestCompleted completed,
        string status)
    {
        await context.Publish(
            completed,
            cfg => cfg.SetRoutingKey(_configuration["RabbitMQ:FileExtractCompletedEvent"] ?? string.Empty),
            context.CancellationToken);

        await SetExtractStatusAsync(completed.FileId, status);
        _logger.LogInformation(
            "Published file extract completion for {FileId}. Success: {IsSuccessful}",
            completed.FileId,
            completed.IsSuccessful);
    }

    private Task SetExtractStatusAsync(Guid fileId, string status) =>
        _cache.SetAsync(FileExtractCache.StatusValueKey(fileId), status, FileExtractCache.StatusLifetime);

    private Task<TrainingFile?> FindTrainingFileAsync(Guid fileId, CancellationToken cancellationToken) =>
        _appDbContext.TrainingFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(file => file.Id == fileId, cancellationToken);

    private async Task<Guid> TryGetTrainingIdAsync(Guid fileId)
    {
        try
        {
            var file = await FindTrainingFileAsync(fileId, CancellationToken.None);
            return file?.TrainingId ?? Guid.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not resolve training id for failed extract {FileId}", fileId);
            return Guid.Empty;
        }
    }

    private async Task MarkFileExtractionStartedAsync(Guid fileId, Guid trainingId, CancellationToken cancellationToken)
    {
        var updated = await _appDbContext.TrainingFiles
            .Where(file => file.Id == fileId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(file => file.IsFileExtractionStarted, true),
                cancellationToken);

        if (updated == 0)
        {
            throw new FileExtractFailedException(trainingId, $"No training file found with Id {fileId}.");
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
                $"Uploaded file '{message.StorageKey}' was not found.", fullPath);
        }

        return fullPath;
    }

    private async Task EnsureVectorDBCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
    {
        if (!await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken))
        {
            try
            {
                _logger.LogInformation("Qdrant collection {Collection} not found. Creating new collection.", collectionName);

                await _qdrantClient.CreateCollectionAsync(
                    collectionName: collectionName,
                    vectorsConfig: new VectorParams
                    {
                        // default output dimension of 'qwen3-embedding:4b'
                        Size = (ulong)int.Parse(_configuration["VectorDB:VectorParams:VectorSize"] ?? "2560"),
                        Distance = Enum.Parse<Distance>(_configuration["VectorDB:VectorParams:Distance"] ?? "Cosine"),
                        Memory = Memory.Cold
                    },
                    quantizationConfig: new QuantizationConfig
                    {
                        Turboquant = new TurboQuantization
                        {
                            Bits = TurboQuantBitSize.Bits8,
                            Memory = Memory.Pinned
                        }
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Qdrant collection {Collection} is ready.", collectionName);
            }
            catch (QdrantException ex) when (IsAlreadyExists(ex))
            {
                _logger.LogInformation(ex, "Qdrant collection {Collection} was created concurrently.", collectionName);
            }
        }
        else
        {
            _logger.LogInformation("Qdrant collection {Collection} already exists.", collectionName);
        }

        try
        {
            await _qdrantClient.CreatePayloadIndexAsync(
                collectionName,
                "file_id",
                PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);
        }
        catch (QdrantException ex) when (IsAlreadyExists(ex))
        {
            _logger.LogDebug("Payload index file_id already exists on {Collection}", collectionName);
        }
    }

    private async Task DeleteFilePointsAsync(string collectionName, Guid fileId, CancellationToken cancellationToken)
    {
        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "file_id",
                        Match = new Match { Keyword = fileId.ToString() }
                    }
                }
            }
        };

        await _qdrantClient.DeleteAsync(collectionName, filter, cancellationToken: cancellationToken);
        _logger.LogInformation("Removed existing Qdrant points for file {FileId} from {Collection}", fileId, collectionName);
    }

    private static bool IsAlreadyExists(QdrantException exception) =>
        exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);

    private sealed class FileExtractFailedException : Exception
    {
        public FileExtractFailedException(Guid trainingId, string message)
            : base(message)
        {
            TrainingId = trainingId;
        }

        public Guid TrainingId { get; }
    }
}

public sealed record DocumentParserSettings(string StorageRootPath);
