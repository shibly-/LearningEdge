using Google.Protobuf.Collections;
using LearningEdge.Application.Caching;
using LearningEdge.Application.Interfaces;
using Microsoft.Extensions.AI;
using Qdrant.Client.Grpc;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LearningEdgeMS.DocumentParserService.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ICacheService _cache;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, ICacheService cache)
    {
        _embeddingGenerator = embeddingGenerator;
        _cache = cache;
    }

    public async Task<List<PointStruct>> ProcessAndEmbedTextAsync(
        Guid fileId,
        string fullText,
        CancellationToken cancellationToken = default)
    {
        // A similarity threshold of 0.70 to 0.75 works well for Qwen3-Embedding.
        List<string> semanticChunks = await SplitTextSemanticallyAsync(
            fullText,
            similarityThreshold: 0.72f,
            cancellationToken);

        var points = new List<PointStruct>(semanticChunks.Count);
        string progressOfFileId = FileExtractCache.ProgressKey(fileId);

        for (int i = 0; i < semanticChunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string chunkText = semanticChunks[i];

            double progressPercentage = Math.Ceiling(((double)(i + 1) / semanticChunks.Count) * 100);
            if (progressPercentage > 100) progressPercentage = 100;
            await SetExtractStatusWithProgressAsync(progressOfFileId, ((int)progressPercentage).ToString());

            // Embed the raw chunk text only. Query instructions belong on the search side.
            var embeddingResult = await _embeddingGenerator.GenerateVectorAsync(chunkText, cancellationToken: cancellationToken);
            float[] vector = embeddingResult.ToArray();

            var payload = new MapField<string, Value>
            {
                { "text", new Value { StringValue = chunkText } },
                { "chunk_type", new Value { StringValue = "semantic" } },
                { "chunk_index", new Value { IntegerValue = i } },
                { "inserted_at", new Value { StringValue = DateTime.UtcNow.ToString("o") } },
                { "file_id", new Value { StringValue = fileId.ToString() } }
            };

            points.Add(new PointStruct
            {
                // Stable id: a later extract of the same file overwrites these points
                // instead of landing on another file's timestamp range.
                Id = ChunkPointId(fileId, i),
                Vectors = vector,
                Payload = { payload }
            });
        }

        return points;
    }

    public async Task<List<string>> SplitTextSemanticallyAsync(
        string text,
        float similarityThreshold,
        CancellationToken cancellationToken = default)
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
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _embeddingGenerator.GenerateVectorAsync(sentence, cancellationToken: cancellationToken);
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

    /// <summary>
    /// UUID derived from the file id and chunk index. Re-indexing the same chunk
    /// keeps the same Qdrant point id.
    /// </summary>
    private static Guid ChunkPointId(Guid fileId, int chunkIndex)
    {
        Span<byte> material = stackalloc byte[20];
        fileId.TryWriteBytes(material);
        BinaryPrimitives.WriteInt32LittleEndian(material[16..], chunkIndex);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(material, hash);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash[..16]);
    }
}
