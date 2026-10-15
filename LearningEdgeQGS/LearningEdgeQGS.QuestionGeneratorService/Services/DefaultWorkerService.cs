using Google.Protobuf.Collections;
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

        // Intentionally left blank.
        // The consumer class will handle all background work.
        //await Task.CompletedTask;

        string prompt = "Generate a multiple choice questions with four options and correct answer on Multi-Factor Authentication?";
        
        string formattedQuery = $"Given a query, retrieve relevant documents that answer the query.\nQuery: {prompt}";

        // Step 1: Embed the prompt
        var embeddingResult = await _embeddingGenerator.GenerateVectorAsync(prompt);
        float[] queryVector = embeddingResult.ToArray();

        // Step 2: Query Qdrant for context
        var searchResults = await _qdrant.QueryAsync(
            collectionName: "test_docs",
            query: queryVector,
            limit: 1,
            //filter: null,                // optional filter
            //scoreThreshold: 0.75f,       // only results above 0.75 similarity
            //offset: 0,
            //payloadSelector: new[] { "title", "text" },
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

        //var contextBuilder = new StringBuilder();
        //foreach (var hit in searchResult)
        //{
        //    contextBuilder.AppendLine(hit.Payload["text"].ToString());
        //}

        // Step 3: Augment prompt with retrieved context
        //string augmentedPrompt = $"{prompt}\n\nContext:\n{contextBuilder}";

        //Console.WriteLine("Generated Questions:");
        //Console.WriteLine(augmentedPrompt);



        /*
        string documentText = """
            To secure your application account, you must enable Multi-Factor Authentication (MFA) in settings. 
            You can use a hardware security key or a software authenticator app. 
            Biometric login options like FaceID can also add a strong extra layer of defense against remote attacks.
            
            Apples and oranges are popular fruits across the world. 
            Oranges are high in Vitamin C and have a thick outer citrus rind. 
            Most commercial apple orchards require cold winter temperatures to produce high-quality yields next spring.
            
            Database clusters must be backed up daily to offsite storage locations. 
            Point-in-time recovery configurations allow you to restore data down to the exact second of a failure. 
            Always encrypt database backups using modern AES-256 standards.
            """;

        Console.WriteLine("Starting semantic text chunking...\n");

        // 2. Perform the semantic split
        // A similarity threshold of 0.70 to 0.75 works well for Qwen3-Embedding
        List<string> semanticChunks = await SplitTextSemanticallyAsync(documentText, similarityThreshold: 0.72f);

        //// 3. Display the intelligently split chunks
        //for (int i = 0; i < semanticChunks.Count; i++)
        //{
        //    Console.WriteLine($"--- [SEMANTIC CHUNK {i + 1}] ---");
        //    Console.WriteLine(semanticChunks[i].Trim());
        //    Console.WriteLine(new string('-', 30) + "\n");
        //}

        await _qdrant.CreateCollectionAsync(
                collectionName: "test_docs",
                vectorsConfig: new VectorParams
                {
                    //default output dimension of 'qwen3-embedding:0.6b'
                    Size = 1024,
                    Distance = Distance.Cosine
                });

        // 5. Generate embeddings and prepare points for Qdrant
        var points = new List<PointStruct>();

        // Generate a unique starting ID baseline based on the current timestamp
        ulong startingId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (int i = 0; i < semanticChunks.Count; i++)
        {
            string chunkText = semanticChunks[i];
            Console.WriteLine($"Generating embedding vector for Semantic Chunk {i + 1}/{semanticChunks.Count}...");

            // CRITICAL: Embed the RAW semantic chunk text only. 
            // Do NOT add query instructions ("Given a query...") during data insertion.
            var embeddingResult = await _embeddingGenerator.GenerateVectorAsync(chunkText);
            float[] vector = embeddingResult.ToArray();

            // Structure the metadata payload so you can identify the source later
            var payload = new MapField<string, Value>
            {
                { "text", new Value { StringValue = chunkText } },
                { "chunk_type", new Value { StringValue = "semantic" } },
                { "inserted_at", new Value { StringValue = DateTime.UtcNow.ToString("o") } }
            };

            // Build the individual point point object
            var point = new PointStruct
            {
                Id = startingId + (ulong)i, // Must be a completely unique ulong number
                Vectors = vector,
                Payload = { payload }
            };

            points.Add(point);
        }

        // 6. Push all points concurrently to your Qdrant cluster
        Console.WriteLine("\nUpserting semantic chunks into Qdrant database...");
        //UpdateResult result = await _embeddingGenerator.UpsertAsync(collectionName, points);
        UpdateResult result = await _qdrant.UpsertAsync("test_docs", points, cancellationToken: stoppingToken);

        Console.WriteLine($"Successfully saved! Server Status: {result.Status}");
        */

        Console.ReadKey();
    }

    private static async Task GenerateAnswerWithLlamaAsync(string userQuery, List<string> retrievedContexts, CancellationToken token)
    {
        // 1. Initialize the local Ollama Chat Client for llama3.1:8b
        IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.1:8b");
        
        // 2. Format the retrieved database chunks into a clear structural string
        var contextBuilder = new StringBuilder();
        for (int i = 0; i < retrievedContexts.Count; i++)
        {
            contextBuilder.AppendLine($"[Document {i + 1}]: {retrievedContexts[i]}");
        }

        // 3. Construct the prompt hierarchy
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

        Console.WriteLine("\n--- Sending request to Llama 3.1:8b ---");

        // 4. Stream the text response back asynchronously for a fluid user interface experience
        await foreach (ChatResponseUpdate streamingChunk in chatClient.GetStreamingResponseAsync(messages))
        {
            // Print out each token immediately as llama3.1 computes it
            Console.Write(streamingChunk.Text);
        }

        Console.WriteLine("\n\n--- End of Generation ---");
    }

    private async Task<List<string>> SplitTextSemanticallyAsync(
        string text,
        float similarityThreshold)
    {
        // Regex to split by sentences (. ! ?) while handling spacing safely
        string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .ToArray();

        if (sentences.Length <= 1) return [text];

        // 1. Generate embeddings for every single individual sentence
        var sentenceVectors = new List<float[]>();
        foreach (var sentence in sentences)
        {
            var result = await _embeddingGenerator.GenerateVectorAsync(sentence);
            sentenceVectors.Add(result.ToArray());
        }

        var chunks = new List<string>();
        var currentChunkSentences = new List<string> { sentences[0] };

        // 2. Compare adjacent sentences to find where topics drift away
        for (int i = 0; i < sentences.Length - 1; i++)
        {
            float similarity = CalculateCosineSimilarity(sentenceVectors[i], sentenceVectors[i + 1]);

            // If similarity falls below our threshold, the topic changed! Commit current chunk and start a new one.
            if (similarity < similarityThreshold)
            {
                chunks.Add(string.Join(" ", currentChunkSentences));
                currentChunkSentences.Clear();
            }

            currentChunkSentences.Add(sentences[i + 1]);
        }

        // Catch the remaining trailing sentences
        if (currentChunkSentences.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunkSentences));
        }

        return chunks;
    }

    private float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        float dotProduct = 0.0f;
        float normA = 0.0f;
        float normB = 0.0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0f || normB == 0.0f) return 0.0f;
        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

}