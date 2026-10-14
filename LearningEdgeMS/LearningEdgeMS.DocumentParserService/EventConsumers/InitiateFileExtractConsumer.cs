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
        _collectionName = _configuration["VectorDB:Qdrant:CollectionName"] ?? "le_docs";
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
        var trainingFile = await _appDbContext.TrainingFiles
            .Where(file => file.Id == message.FileId)
            .FirstOrDefaultAsync(context.CancellationToken);

        if(trainingFile is null)
        {
            _logger.LogWarning("Training file with ID {FileId} not found in the database.", message.FileId);
            return;
        }

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

            var collectionName = _collectionName + "_" + trainingFile.TrainingId;

            // Ensure the Qdrant collection exists before inserting points
            await EnsureVectorDBCollectionExistsAsync(collectionName);

            // Upsert points into Qdrant
            await _qdrantClient.UpsertAsync(collectionName, points, cancellationToken: context.CancellationToken);

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
