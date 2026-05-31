namespace FinDocReview.Core.Entities;

public class DocumentChunk
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public int TokenCount { get; set; }
    public string? EmbeddingJson { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}