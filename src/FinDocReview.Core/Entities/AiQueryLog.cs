namespace FinDocReview.Core.Entities;

public class AiQueryLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? DocumentId { get; set; }
    public required string UserId { get; set; }
    public required string UserPrompt { get; set; }
    public required string AiResponse { get; set; }
    public string? SourceChunkIdsJson { get; set; }
    public required string ModelUsed { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int LatencyMs { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Document? Document { get; set; }
}