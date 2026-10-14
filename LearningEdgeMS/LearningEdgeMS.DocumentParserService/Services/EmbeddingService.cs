using Microsoft.Extensions.AI;

namespace LearningEdgeMS.DocumentParserService.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<List<(string ChunkText, float[] Vector)>> ProcessAndEmbedTextAsync(string fullText)
    {
        // Chunk the document text into smaller segments
        var chunks = ChunkText(fullText, maxChunkLength: 1000, overlap: 100);
        var results = new List<(string, float[])>();

        // Generate embeddings for all chunks
        // Microsoft.Extensions.AI natively supports batch processing for efficiency
        GeneratedEmbeddings<Embedding<float>> embeddings = await _embeddingGenerator.GenerateAsync(chunks);

        // Map the generated vectors back to their respective text chunks
        for (int i = 0; i < chunks.Count; i++)
        {
            float[] vector = embeddings[i].Vector.ToArray();
            results.Add((chunks[i], vector));
        }

        return results;
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
}