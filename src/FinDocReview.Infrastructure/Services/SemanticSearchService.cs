using System.Text.Json;
using FinDocReview.Core.Entities;
using FinDocReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001

namespace FinDocReview.Infrastructure.Services;

public class SemanticSearchService
{
    private readonly AppDbContext _db;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public SemanticSearchService(
        AppDbContext db,
        ITextEmbeddingGenerationService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task<List<DocumentChunk>> FindRelevantChunksAsync(
        Guid documentId,
        string query,
        int topK = 5,
        CancellationToken ct = default)
    {
        // Embed the query
        var queryEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(
            [query], cancellationToken: ct);
        var queryVector = queryEmbeddings[0].ToArray();

        // Load all chunks for this document that have embeddings
        var chunks = await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId && c.EmbeddingJson != null)
            .ToListAsync(ct);

        if (chunks.Count == 0)
            return [];

        // Compute cosine similarity in memory
        var scored = chunks
            .Select(c => new
            {
                Chunk = c,
                Score = EmbeddingService.ComputeCosineSimilarity(
                    queryVector,
                    EmbeddingService.DeserializeEmbedding(c.EmbeddingJson!))
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        return scored;
    }
}