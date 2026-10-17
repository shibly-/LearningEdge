using Google.Protobuf.Collections;
using LearningEdge.Domain.Entities.Questions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using static Qdrant.Client.Grpc.Qdrant;

namespace LearningEdgeMS.QuestionGeneratorService.Services;

public class DefaultWorkerService : BackgroundService
{
    private readonly ILogger<DefaultWorkerService> _logger;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly Qdrant.Client.QdrantClient _qdrant;

    public DefaultWorkerService(
        ILogger<DefaultWorkerService> logger, 
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        Qdrant.Client.QdrantClient qdrant)
    {
        _logger = logger;        
        _embeddingGenerator = embeddingGenerator;
        _qdrant = qdrant;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuestionGenerator service has started at: {time}", DateTimeOffset.Now);

        // The consumer class will handle all background work.
        // await Task.CompletedTask;

        Console.Write("Enter the collection name: ");
        string collectionName = Console.ReadLine() ?? string.Empty; // le_docs_5839a1a37b8942779aaff1edaafca46d
        Console.Write("\nDatabase (Qdrant) collection " + collectionName + " will be used for this query...");

        Console.Write("\n\nAsk for a question: ");
        string question = Console.ReadLine() ?? string.Empty;

        string prompt = question ?? "Generate a summary of this document?";
                
        string formattedQuery = $"Given a query, retrieve relevant documents that answer the query.\nQuery: {prompt}";

        // Embed the prompt
        var embeddingResult = await _embeddingGenerator.GenerateVectorAsync(prompt);
        float[] queryVector = embeddingResult.ToArray();

        // Query Qdrant for context
        var searchResults = await _qdrant.QueryAsync(
            collectionName: collectionName,
            query: queryVector,
            limit: 10,
            vectorsSelector: false,
            cancellationToken: stoppingToken
        );

        List<string> retrievedContexts = new List<string>();
        Console.WriteLine("\n--- Search Results ---");
        foreach (ScoredPoint point in searchResults)
        {
            Console.WriteLine($"Score: {point.Score:F4}");

            // Extract the actual text string you stored in the database
            if (point.Payload.TryGetValue("text", out var textValue))
            {
                retrievedContexts.Add(textValue.StringValue);
                Console.WriteLine($"Content: {textValue.StringValue}");
            }
        }

        using var _ = GenerateAnswerWithLlamaAsync(prompt, retrievedContexts, stoppingToken);

        Console.ReadKey();
    }

    private static async Task GenerateAnswerWithLlamaAsync(string userQuery, List<string> retrievedContexts, CancellationToken token)
    {
        // Initialize the local Ollama Chat Client for llama3.1:8b
        IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.1:8b");
        
        // Format the retrieved database chunks into a clear structural string
        var contextBuilder = new StringBuilder();
        for (int i = 0; i < retrievedContexts.Count; i++)
        {
            contextBuilder.AppendLine($"[Document {i + 1}]: {retrievedContexts[i]}");
        }

        // Construct the prompt hierarchy
        // System message instructs the LLM how to behave and provides facts
        // User message contains the original raw question
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, $"""
                You are a helpful assistant. You must answer the user's question using ONLY the provided document contexts below. 
                If the answer cannot be found in the contexts, politely state that you do not know. Do not hallucinate or make up facts.
                
                --- RETRIEVED CONTEXT DOCUMENTS ---
                {contextBuilder}
                """),
            new ChatMessage(ChatRole.User, userQuery)
        };

        Console.WriteLine("\n--- Sending request to llama3.1:8b ==>");
        Console.WriteLine("\n--- Actual question from user:\n" + userQuery);

        Console.WriteLine("\n\n--- Model (llama3.1:8b) has generated the following answer: \n");
        // Stream the text response back asynchronously for a fluid user interface experience
        await foreach (ChatResponseUpdate streamingChunk in chatClient.GetStreamingResponseAsync(messages))
        {
            // Print out each token immediately as llama3.1 computes it
            Console.Write(streamingChunk.Text);
        }

        Console.WriteLine("\n\n--- End of generation ---");
    }

}