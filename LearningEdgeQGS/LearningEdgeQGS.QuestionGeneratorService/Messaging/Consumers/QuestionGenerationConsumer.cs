using LearningEdge.Application.Events;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Messaging.Contracts.Events;
using LearningEdgeMS.QuestionGeneratorService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Diagnostics;


namespace LearningEdgeMS.QuestionGeneratorService.Messaging.Consumers;

public sealed class QuestionGenerationConsumer : IConsumer<IQuestionGenerationSubmitted>
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _appDbContext;        
    private readonly QdrantClient _qdrantClient;
    private readonly QuestionGenerationService _questionGenerationService;    
    private readonly ILogger<QuestionGenerationConsumer> _logger;
    private readonly string _collectionName;

    public QuestionGenerationConsumer(
        IConfiguration configuration,
        ApplicationDbContext appDbContext,
        QuestionGenerationService questionGenerationService,        
        QdrantClient qdrantClient,
        ILogger<QuestionGenerationConsumer> logger)
    {
        _configuration = configuration;
        _collectionName = _configuration["VectorDB:Qdrant:CollectionName"] ?? "le_docs";
        _appDbContext = appDbContext;                
        _qdrantClient = qdrantClient;
        _questionGenerationService = questionGenerationService;
        _logger = logger;
    }

    // <summary>
    // Consumes the IQuestionGenerationSubmitted event and initiates the question generation process.
    // </summary>

    public async Task Consume(ConsumeContext<IQuestionGenerationSubmitted> context)
    {
        var message = context.Message;
        Console.WriteLine($"Received message => TrainingId: {message.TrainingId}, PromptRequestId: {message.PromptRequestId}, PromptText: {message.PromptText} at {DateTimeOffset.Now}");
        // will implement later
        await Task.Delay(1000);
        await Task.CompletedTask;
    }
}
