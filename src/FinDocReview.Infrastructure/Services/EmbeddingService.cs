using System.Text.Json;
using FinDocReview.Core.Entities;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001

namespace FinDocReview.Infrastructure.Services;

public class EmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public EmbeddingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task GenerateEmbeddingsAsync(
        List<DocumentChunk> chunks,
        CancellationToken ct = default)
    {
        var batches = chunks.Chunk(20);

        foreach (var batch in batches)
        {
            var texts = batch.Select(c => c.Content).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken: ct);

            for (var i = 0; i < batch.Length; i++)
            {
                var vector = embeddings[i].ToArray();
                batch[i].EmbeddingJson = JsonSerializer.Serialize(vector);
            }

            await Task.Delay(200, ct);
        }
    }

    public static float ComputeCosineSimilarity(float[] a, float[] b)
    {
        var dot = a.Zip(b, (x, y) => x * y).Sum();
        var magA = MathF.Sqrt(a.Sum(x => x * x));
        var magB = MathF.Sqrt(b.Sum(x => x * x));
        return magA == 0 || magB == 0 ? 0 : dot / (magA * magB);
    }

    public static float[] DeserializeEmbedding(string json)
    {
        return JsonSerializer.Deserialize<float[]>(json) ?? [];
    }
}