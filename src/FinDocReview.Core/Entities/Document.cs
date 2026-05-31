namespace FinDocReview.Core.Entities;

public class Document
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? BlobUri { get; set; }
    public string? LocalPath { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public required string UploadedById { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];
    public DocumentSummary? Summary { get; set; }
    public ICollection<AiQueryLog> QueryLogs { get; set; } = [];
}

public enum DocumentStatus { Pending, Processing, Completed, Failed }