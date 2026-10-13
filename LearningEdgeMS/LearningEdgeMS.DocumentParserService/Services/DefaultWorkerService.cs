using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace LearningEdgeMS.DocumentParserService.Services;

public class DefaultWorkerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DefaultWorkerService> _logger;    
    private readonly QdrantClient _qdrantClient;
    private readonly string _collectionName;

    public DefaultWorkerService(IConfiguration configuration, ILogger<DefaultWorkerService> logger, QdrantClient qdrantClient)
    {
        _configuration = configuration;
        _collectionName = _configuration["VectorDB:Qdrant:CollectionName"] ?? "le_documents";
        _logger = logger;        
        _qdrantClient = qdrantClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentParser service has started at: {time}", DateTimeOffset.Now);

        // Ensure Qdrant collection exists before processing
        await EnsureVectorDBCollectionExistsAsync();

        _logger.LogInformation("Qdrant collection '{collection}' is ready to use...", _collectionName);
    }

    private async Task EnsureVectorDBCollectionExistsAsync()
    {
        var collections = await _qdrantClient.ListCollectionsAsync();
        if (!collections.Contains(_collectionName))
        {
            _logger.LogInformation("Qdrant collection '{collection}' not found. Creating new collection...", _collectionName);
            
            await _qdrantClient.CreateCollectionAsync(
                collectionName: _collectionName,
                vectorsConfig: new VectorParams
                {
                    //default output dimension of 'qwen3-embedding:0.6b'
                    Size = (ulong)int.Parse(_configuration["VectorDB:VectorParams:VectorSize"] ?? "1024"),                     
                    Distance = Enum.Parse<Distance>(_configuration["VectorDB:VectorParams:Distance"] ?? "Cosine")
                });
        }
        else
        {
            _logger.LogInformation("Qdrant collection '{collection}' already exists.", _collectionName);
        }
    }
}