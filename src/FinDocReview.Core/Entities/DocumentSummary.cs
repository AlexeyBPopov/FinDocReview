namespace FinDocReview.Core.Entities;

public class DocumentSummary
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public required string Summary { get; set; }
    public string? KeyRisksJson { get; set; }
    public string? ActionItemsJson { get; set; }
    public required string ModelUsed { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}