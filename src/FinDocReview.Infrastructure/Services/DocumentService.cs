using FinDocReview.Core.Entities;
using FinDocReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinDocReview.Infrastructure.Services;

public class DocumentService
{
    private readonly AppDbContext _db;
    private readonly LocalStorageService _storage;

    public DocumentService(AppDbContext db, LocalStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Document> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string userId,
        CancellationToken ct = default)
    {
        var localPath = await _storage.SaveFileAsync(fileStream, fileName, ct);

        var document = new Document
        {
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            LocalPath = localPath,
            UploadedById = userId,
            Status = DocumentStatus.Pending
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);

        return document;
    }

    public async Task<List<Document>> GetUserDocumentsAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Documents
            .Where(d => d.UploadedById == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<Document?> GetDocumentAsync(Guid id, string userId, CancellationToken ct = default)
    {
        return await _db.Documents
            .Include(d => d.Summary)
            .Include(d => d.QueryLogs.OrderByDescending(q => q.CreatedAt).Take(10))
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedById == userId, ct);
    }
}