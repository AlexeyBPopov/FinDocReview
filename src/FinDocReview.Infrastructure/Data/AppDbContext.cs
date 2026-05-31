using FinDocReview.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinDocReview.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentSummary> DocumentSummaries => Set<DocumentSummary>();
    public DbSet<AiQueryLog> AiQueryLogs => Set<AiQueryLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            e.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(d => d.UploadedById).HasMaxLength(450).IsRequired();
            e.HasOne(d => d.Summary)
             .WithOne(s => s.Document)
             .HasForeignKey<DocumentSummary>(s => s.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(d => d.Chunks)
             .WithOne(c => c.Document)
             .HasForeignKey(c => c.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(d => d.QueryLogs)
             .WithOne(q => q.Document)
             .HasForeignKey(q => q.DocumentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DocumentChunk>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.DocumentId);
        });

        builder.Entity<DocumentSummary>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.ModelUsed).HasMaxLength(100).IsRequired();
        });

        builder.Entity<AiQueryLog>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasIndex(q => q.DocumentId);
            e.HasIndex(q => q.CreatedAt);
            e.Property(q => q.ModelUsed).HasMaxLength(100).IsRequired();
            e.Property(q => q.UserId).HasMaxLength(450).IsRequired();
        });
    }
}