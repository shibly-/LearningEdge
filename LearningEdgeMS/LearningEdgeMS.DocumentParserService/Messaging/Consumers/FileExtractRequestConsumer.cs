using LearningEdge.Application.Caching;
using LearningEdge.Application.Events;
using LearningEdge.Application.Interfaces;
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
    private readonly DocumentParsingService _parserService;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<FileExtractRequestConsumer> _logger;
    private readonly ICacheService _cache;
    private readonly string _collectionName;

    public FileExtractRequestConsumer(
        IConfiguration configuration,
        ApplicationDbContext appDbContext,        
        DocumentParserSettings settings,
        DocumentParsingService parserService,
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
        _parserService = parserService;
        _embeddingService = embeddingService;
        _cache = cache;
        _logger = logger;
    }

    // <summary>
    // Consumes the IExtractRequestSubmitted event and initiates the file extraction process.
    // </summary>

    public async Task Consume(ConsumeContext<IExtractRequestSubmitted> context)
    {
        var message = context.Message;

        var fileStatusInCache = await _cache.GetAsync(FileExtractCache.StatusValueKey(message.FileId));

        if (fileStatusInCache != null && fileStatusInCache == "Completed")
        {
            _logger.LogInformation($"File extraction already completed for FileId: {message.FileId}. Skipping processing.");
            return;
        }
        else
        {
            _logger.LogInformation($"Starting file extraction for FileId: {message.FileId} with status: {fileStatusInCache}");
        }

        await _cache.SetAsync(FileExtractCache.ProgressKey(message.FileId), "0", FileExtractCache.StatusLifetime);
        await SetExtractStatusAsync(message.FileId, FileExtractCache.Status.Processing);

        string filePath;
        try
        {
            filePath = ResolveStoredFile(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve stored file for extract request {FileId}", message.FileId);
            await SetExtractStatusAsync(message.FileId, FileExtractCache.Status.Failed);
            throw;
        }
        var fileName = string.IsNullOrWhiteSpace(message.FileName)
            ? Path.GetFileName(filePath)
            : message.FileName;

        _logger.LogInformation("Received extract request for file {fileName} ({FileId}) uploaded by {UploadedByUserId}", fileName, message.FileId, message.UploadedByUserId);

        // Check the file exists before proceeding
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Target file not found: {filePath}", filePath);
            await SetExtractStatusAsync(message.FileId, FileExtractCache.Status.Failed);
            return;
        }

        _logger.LogInformation("Starting extraction for {FileName} in {filePath} ({FileId})", fileName, filePath, message.FileId);

        // Mark the file as being extracted
        var trainingFile = await _appDbContext.TrainingFiles
            .Where(file => file.Id == message.FileId)            
            .FirstOrDefaultAsync(context.CancellationToken);

        if(trainingFile is null)
        {
            _logger.LogWarning("Training file with ID {FileId} not found in the database.", message.FileId);
            await SetExtractStatusAsync(message.FileId, FileExtractCache.Status.Failed);
            return;
        }

        await MarkFileExtractionStartedAsync(message.FileId, context.CancellationToken);

        IExtractRequestCompleted? responseMessage = null;

        // Start the extraction process
        try
        {
            _logger.LogInformation("Parsing file: {filePath}", filePath);

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Extract raw text dynamically based on extension
            string rawText = await _parserService.ExtractTextAsync(filePath);

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("Parsing - Elapsed Time (TimeSpan): {ElapsedTime}", elapsedTime);
            _logger.LogInformation("Parsing - Elapsed Milliseconds: {ElapsedMilliseconds} ms", elapsedMilliseconds);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("No text extracted from file: {filePath}", filePath);
                responseMessage = new ExtractRequestCompleted(
                    FileId: message.FileId,
                    TrainingId: trainingFile.TrainingId,
                    IsSuccessful: false,
                    FailedReason: "No text extracted from file.");
                return;
            }

            _logger.LogInformation("Generating chunks and embedding vectors...");

            // Chunk and generate vectors via Ollama & Microsoft.Extensions.AI
            var points = await _embeddingService.ProcessAndEmbedTextAsync(message.FileId, rawText);

            stopwatch.Restart();
            TimeSpan elapsedTimeNext = stopwatch.Elapsed;
            long elapsedMillisecondsNext = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("Embedding - Elapsed Time (TimeSpan): {ElapsedTime}", elapsedTimeNext);
            _logger.LogInformation("Embedding - Elapsed Milliseconds: {ElapsedMilliseconds} ms", elapsedMillisecondsNext);

            _logger.LogInformation("Storing {count} chunks into Qdrant vector database...", points.Count);

            
            var training = await _appDbContext.Trainings
            .Where(training => training.Id == trainingFile.TrainingId)
            .FirstOrDefaultAsync(context.CancellationToken);

            if (training is null)
            {
                _logger.LogWarning("Training with ID {TrainingId} not found in the database.", trainingFile.TrainingId);
                responseMessage = new ExtractRequestCompleted(
                    FileId: message.FileId,
                    TrainingId: trainingFile.TrainingId,
                    IsSuccessful: false,
                    FailedReason: "Training not found.");
                return;
            }

            var collectionName = _collectionName + "_" + training.OrganizationId.ToString("N");
            _logger.LogInformation("Qdrant collection '{collection}' will be used to store {count} chunks...", collectionName, points.Count);

            // Ensure the Qdrant collection exists before inserting points
            await EnsureVectorDBCollectionExistsAsync(collectionName);

            // Upsert points into Qdrant
            UpdateResult result = await _qdrantClient.UpsertAsync(collectionName, points, cancellationToken: context.CancellationToken);

            _logger.LogInformation("Successfully indexed and saved document into Qdrant!");

            responseMessage = new ExtractRequestCompleted(
                FileId: message.FileId,
                TrainingId: trainingFile.TrainingId,
                IsSuccessful: true,
                FailedReason: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the document ingestion pipeline.");

            responseMessage = new ExtractRequestCompleted(
                FileId: message.FileId,
                TrainingId: trainingFile.TrainingId,
                IsSuccessful: false,
                FailedReason: ex.Message
            );
        }
        finally
        {
            if (responseMessage is not null)
            {
                await SetExtractStatusAsync(
                    message.FileId,
                    responseMessage.IsSuccessful
                        ? FileExtractCache.Status.Completed
                        : FileExtractCache.Status.Failed);

                // Publish as IExtractRequestCompleted so the message is routed to the
                // exchange the Web API queue le-file-extract-completed is bound to.
                await context.Publish(
                    responseMessage,
                    cfg => cfg.SetRoutingKey(_configuration["RabbitMQ:FileExtractCompletedEvent"] ?? string.Empty),
                    context.CancellationToken);

                _logger.LogInformation("Published file extract completion event for File ID: {FileId}!", message.FileId);
            }
        }
    }

    private Task SetExtractStatusAsync(Guid fileId, string status) =>
        _cache.SetAsync(FileExtractCache.StatusValueKey(fileId), status, FileExtractCache.StatusLifetime);

    // Helper method to resolve the stored file path based on the message
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

    // Resolves the stored file path based on the message details
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

    // Ensure the Qdrant collection exists before inserting points
    private async Task EnsureVectorDBCollectionExistsAsync(string collectionName)
    {
        var collections = await _qdrantClient.ListCollectionsAsync();
        if (!collections.Contains(collectionName))
        {
            _logger.LogInformation("Qdrant collection '{collection}' not found. Creating new collection...", collectionName);

            await _qdrantClient.CreateCollectionAsync(
                collectionName: collectionName,
                vectorsConfig: new VectorParams
                {
                    //default output dimension of 'qwen3-embedding:0.6b'
                    Size = (ulong)int.Parse(_configuration["VectorDB:VectorParams:VectorSize"] ?? "1024"),
                    Distance = Enum.Parse<Distance>(_configuration["VectorDB:VectorParams:Distance"] ?? "Cosine")
                });
            _logger.LogInformation("Qdrant collection '{collection}' is ready to use...", collectionName);
        }
        else
        {
            _logger.LogInformation("Qdrant collection '{collection}' already exists.", collectionName);
        }
    }
}

public sealed record DocumentParserSettings(string StorageRootPath);
