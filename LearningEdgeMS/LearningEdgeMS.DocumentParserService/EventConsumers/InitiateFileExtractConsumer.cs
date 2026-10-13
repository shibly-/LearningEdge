using LearningEdge.Application.Interfaces;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Messaging.Contracts.Events;
using LearningEdgeMS.DocumentParserService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;


namespace LearningEdgeMS.DocumentParserService.EventConsumers;

public sealed class InitiateFileExtractConsumer : IConsumer<IExtractRequestSubmitted>
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _appDbContext;    
    private readonly DocumentParserSettings _settings;
    private readonly QdrantClient _qdrantClient;
    private readonly DocumentParsingService _parserService;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<InitiateFileExtractConsumer> _logger;
    private readonly string _collectionName;

    public InitiateFileExtractConsumer(
        IConfiguration configuration,
        ApplicationDbContext appDbContext,        
        DocumentParserSettings settings,
        DocumentParsingService parserService,
        EmbeddingService embeddingService,
        QdrantClient qdrantClient,
        ILogger<InitiateFileExtractConsumer> logger)
    {
        _configuration = configuration;
        _collectionName = _configuration["VectorDB:Qdrant:CollectionName"] ?? "le_documents";
        _appDbContext = appDbContext;        
        _settings = settings;
        _qdrantClient = qdrantClient;
        _parserService = parserService;
        _embeddingService = embeddingService;
        _logger = logger;        
    }

    public async Task Consume(ConsumeContext<IExtractRequestSubmitted> context)
    {
        var message = context.Message;
        var filePath = ResolveStoredFile(message);
        var fileName = string.IsNullOrWhiteSpace(message.FileName)
            ? Path.GetFileName(filePath)
            : message.FileName;

        _logger.LogInformation($"Received extract request for file {fileName} ({message.FileId}) uploaded by {message.UploadedByUserId}");

        // Check the file exists before proceeding
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Target file not found: {filePath}", filePath);
            return;
        }

        _logger.LogInformation("Starting extraction for {FileName} in {filePath} ({FileId})", fileName, filePath, message.FileId);

        // Mark the file as being extracted
        await MarkFileExtractionStartedAsync(message.FileId, context.CancellationToken);

        // Start the extraction process
        try
        {
            _logger.LogInformation("Parsing file: {filePath}", filePath);

            // Extract raw text dynamically based on extension
            string rawText = await _parserService.ExtractTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("No text extracted from file: {filePath}", filePath);
                return;
            }

            _logger.LogInformation("Generating chunks and embedding vectors...");

            // Chunk and generate vectors via Ollama & Microsoft.Extensions.AI
            var embeddedChunks = await _embeddingService.ProcessAndEmbedTextAsync(rawText);

            _logger.LogInformation("Storing {count} chunks into Qdrant vector database...", embeddedChunks.Count);

            // Map chunks to Qdrant PointStruct objects with payload metadata
            var points = new List<PointStruct>();
            ulong idCounter = (ulong)DateTime.UtcNow.Ticks; // Generate unique base ID

            foreach (var (chunkText, vector) in embeddedChunks)
            {
                points.Add(new PointStruct
                {
                    Id = idCounter++,
                    Vectors = vector,
                    Payload =
                    {
                        ["text"] = chunkText,
                        ["source_file"] = Path.GetFileName(filePath),
                        ["ingested_at"] = DateTime.UtcNow.ToString("o")
                    }
                });
            }

            // Upsert points into Qdrant
            await _qdrantClient.UpsertAsync(_collectionName, points, cancellationToken: context.CancellationToken);

            _logger.LogInformation("Successfully indexed and saved document into Qdrant!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the document ingestion pipeline.");
        }
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
                $"Uploaded file '{message.StorageKey}' was not found.", fullPath);
        }

        return fullPath;
    }
}

public sealed record DocumentParserSettings(string StorageRootPath);
