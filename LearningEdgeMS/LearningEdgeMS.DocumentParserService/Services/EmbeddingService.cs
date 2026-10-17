using Google.Protobuf.Collections;
using LearningEdge.Application.Caching;
using LearningEdge.Application.Interfaces;
using Microsoft.Extensions.AI;
using OllamaSharp.Models.Chat;
using Qdrant.Client.Grpc;
using System.Text.RegularExpressions;

namespace LearningEdgeMS.DocumentParserService.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ICacheService _cache;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, ICacheService cache   )
    {
        _embeddingGenerator = embeddingGenerator;
        _cache = cache;
    }

    public async Task<List<PointStruct>> ProcessAndEmbedTextAsync(Guid fileId,string fullText)
    {
        // Chunk the document text into smaller segments
        //var chunks = ChunkText(fullText, maxChunkLength: 1000, overlap: 100);
        //var results = new List<(string, float[])>();

        // Perform the semantic split
        // A similarity threshold of 0.70 to 0.75 works well for Qwen3-Embedding
        List<string> semanticChunks = await SplitTextSemanticallyAsync(fullText, similarityThreshold: 0.72f);

        // Generate embeddings and prepare points for Qdrant
        var points = new List<PointStruct>();

        // Generate a unique starting ID baseline based on the current timestamp
        ulong startingId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string progressOfFileId = FileExtractCache.StatusKey(fileId) + ":progress";
        

        for (int i = 0; i < semanticChunks.Count; i++)
        {
            string chunkText = semanticChunks[i];
            Console.WriteLine($"Generating embedding vector for Semantic Chunk {i + 1}/{semanticChunks.Count}...");

            double progressPercentage = ((double)(i + 1)/ ((double)semanticChunks.Count)) * 100;
            progressPercentage = Math.Ceiling(progressPercentage);
            if (progressPercentage > 100) progressPercentage = 100;
            Console.WriteLine($"progressPercentage => {progressPercentage}");
            await SetExtractStatusWithProgressAsync(progressOfFileId, ((int)progressPercentage).ToString());

            // CRITICAL: Embed the RAW semantic chunk text only. 
            // Do NOT add query instructions ("Given a query...") during data insertion.

            // Generate embeddings for all chunks
            // Microsoft.Extensions.AI natively supports batch processing for efficiency
            //GeneratedEmbeddings<Embedding<float>> embeddings = await _embeddingGenerator.GenerateAsync(semanticChunks);

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

        //// Map the generated vectors back to their respective text chunks
        //for (int i = 0; i < semanticChunks.Count; i++)
        //{
        //    float[] vector = embeddings[i].Vector.ToArray();
        //    results.Add((semanticChunks[i], vector));
        //}

        return points;
    }

    public async Task<List<string>> SplitTextSemanticallyAsync(
        string text,
        float similarityThreshold)
    {
        // Regex to split by sentences (. ! ?) while handling spacing safely
        string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .ToArray();

        if (sentences.Length <= 1) return [text];

        // Generate embeddings for every single individual sentence
        var sentenceVectors = new List<float[]>();
        foreach (var sentence in sentences)
        {
            var result = await _embeddingGenerator.GenerateVectorAsync(sentence);
            sentenceVectors.Add(result.ToArray());
        }

        var chunks = new List<string>();
        var currentChunkSentences = new List<string> { sentences[0] };

        // Compare adjacent sentences to find where topics drift away
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

    private static List<string> ChunkText(string text, int maxChunkLength, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var step = Math.Max(1, maxChunkLength - overlap);
        var chunks = new List<string>(text.Length / step + 1);
        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var length = Math.Min(maxChunkLength, text.Length - startIndex);

            // Break on a space when this window is not the tail of the document.
            if (startIndex + length < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', startIndex + length - 1, length);
                if (lastSpace > startIndex)
                {
                    length = lastSpace - startIndex + 1;
                }
            }

            var chunk = text.AsSpan(startIndex, length).Trim();
            if (!chunk.IsEmpty)
            {
                chunks.Add(chunk.ToString());
            }

            if (startIndex + length >= text.Length)
            {
                break;
            }

            // A chunk shorter than the overlap must still move forward.
            // Resetting to zero here re-walks the whole document.
            var nextStart = startIndex + length - overlap;
            startIndex = nextStart > startIndex ? nextStart : startIndex + length;
        }

        return chunks;
    }

    private Task SetExtractStatusWithProgressAsync(string progressOfFileId, string progress)
        => _cache.SetAsync(progressOfFileId, progress, FileExtractCache.StatusLifetime);
}