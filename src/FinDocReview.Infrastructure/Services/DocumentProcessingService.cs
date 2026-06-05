using FinDocReview.Core.Entities;
using FinDocReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace FinDocReview.Infrastructure.Services;

public class DocumentProcessingService : BackgroundService
{
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(100);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task EnqueueAsync(Guid documentId, CancellationToken ct = default)
    {
        await _queue.Writer.WriteAsync(documentId, ct);
        _logger.LogInformation("Document {DocumentId} enqueued for processing", documentId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document processing service started");

        await foreach (var documentId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessDocumentAsync(documentId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing document {DocumentId}", documentId);
            }
        }
    }

    private async Task ProcessDocumentAsync(Guid documentId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var textExtractor = scope.ServiceProvider.GetRequiredService<TextExtractionService>();
        var chunkingService = scope.ServiceProvider.GetRequiredService<ChunkingService>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();
        var summarizationService = scope.ServiceProvider.GetRequiredService<AiSummarizationService>();

        var document = await db.Documents.FindAsync([documentId], ct);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", documentId);
            return;
        }

        try
        {
            // 1. Update status
            document.Status = DocumentStatus.Processing;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Processing document {DocumentId}: {FileName}", documentId, document.FileName);

            // 2. Extract text
            var filePath = document.LocalPath ?? document.BlobUri
                ?? throw new InvalidOperationException("Document has no file path");
            var text = await textExtractor.ExtractTextAsync(
                document.LocalPath,
                document.BlobUri,
                document.FileName,
                ct);

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text could be extracted from document");

            // 3. Chunk
            var chunks = chunkingService.CreateChunks(documentId, text);
            _logger.LogInformation("Created {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);

            // 4. Generate embeddings
            await embeddingService.GenerateEmbeddingsAsync(chunks, ct);

            // 5. Save chunks
            db.DocumentChunks.AddRange(chunks);
            await db.SaveChangesAsync(ct);

            // 6. Summarize
            var summary = await summarizationService.SummarizeAsync(documentId, text, ct);
            db.DocumentSummaries.Add(summary);

            // 7. Mark completed
            document.Status = DocumentStatus.Completed;
            document.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Document {DocumentId} processed successfully", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
        }
    }
}