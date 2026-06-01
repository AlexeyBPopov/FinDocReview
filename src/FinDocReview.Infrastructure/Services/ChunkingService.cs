using FinDocReview.Core.Entities;

namespace FinDocReview.Infrastructure.Services;

public class ChunkingService
{
    private const int ChunkSize = 2000;    // characters (~500 tokens)
    private const int ChunkOverlap = 200;  // characters overlap between chunks

    public List<DocumentChunk> CreateChunks(Guid documentId, string text)
    {
        var chunks = new List<DocumentChunk>();
        var cleanText = CleanText(text);

        if (string.IsNullOrWhiteSpace(cleanText))
            return chunks;

        var index = 0;
        var chunkIndex = 0;

        while (index < cleanText.Length)
        {
            var length = Math.Min(ChunkSize, cleanText.Length - index);
            var content = cleanText.Substring(index, length);

            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                ChunkIndex = chunkIndex,
                Content = content,
                TokenCount = EstimateTokenCount(content)
            });

            chunkIndex++;
            index += ChunkSize - ChunkOverlap;
        }

        return chunks;
    }

    private static string CleanText(string text)
    {
        // Remove excessive whitespace and normalize line endings
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));
        return string.Join(" ", lines);
    }

    private static int EstimateTokenCount(string text)
    {
        // Rough estimate: 1 token ≈ 4 characters
        return text.Length / 4;
    }
}