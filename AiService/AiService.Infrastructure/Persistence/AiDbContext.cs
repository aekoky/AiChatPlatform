using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AiService.Infrastructure.Persistence;

public class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<DocumentChunkEntity> DocumentChunks => Set<DocumentChunkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rag");
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<DocumentChunkEntity>(entity =>
        {
            entity.ToTable("document_chunks");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(768)");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasConversion(
            v => v == null ? null : JsonSerializer.Serialize(v),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<DocumentChunkMetadata>(v));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Embedding)
                  .HasMethod("hnsw")
                  .HasOperators("vector_cosine_ops");
        });
    }
}
