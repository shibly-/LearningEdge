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
        // 1. Chunk the document text into smaller segments
        var chunks = ChunkText(fullText, maxChunkLength: 1000, overlap: 100);
        var results = new List<(string, float[])>();

        // 2. Generate embeddings for all chunks
        // Microsoft.Extensions.AI natively supports batch processing for efficiency
        GeneratedEmbeddings<Embedding<float>> embeddings = await _embeddingGenerator.GenerateAsync(chunks);

        // 3. Map the generated vectors back to their respective text chunks
        for (int i = 0; i < chunks.Count; i++)
        {
            float[] vector = embeddings[i].Vector.ToArray();
            results.Add((chunks[i], vector));
        }

        return results;
    }

    private List<string> ChunkText(string text, int maxChunkLength, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int startIndex = 0;
        while (startIndex < text.Length)
        {
            int length = Math.Min(maxChunkLength, text.Length - startIndex);

            // Try to break cleanly at a space if we are in the middle of a document
            if (startIndex + length < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', startIndex + length, length);
                if (lastSpace > startIndex)
                {
                    length = lastSpace - startIndex + 1;
                }
            }

            string chunk = text.Substring(startIndex, length).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            startIndex += length - overlap;
            if (startIndex < 0) startIndex = 0;
        }

        return chunks;
    }
}