using DocumentIngestion.Application.Domain;
using DocumentIngestion.Application.Services;
using DocumentIngestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
namespace DocumentIngestion.Infrastructure.Repositories;

public class PgVectorRepository(IngestionDbContext context) : IVectorStoreRepository
{
    public async Task SaveChunksAsync(
        List<DocumentChunk> chunks,
        Guid? userId,
        Guid? sessionId,
        string scope,
        CancellationToken ct = default)
    {
        var entities = chunks.Select(chunk => new DocumentChunkEntity
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            UserId = userId,
            SessionId = sessionId,
            Scope = scope,
            Content = chunk.Content,
            Embedding = chunk.Embedding is not null ? new Vector(chunk.Embedding) : null,
            ChunkIndex = chunk.ChunkIndex,
            CreatedAt = DateTime.UtcNow,
            Metadata = new DocumentChunkMetadataEntity
            {
                FileName = chunk.Metadata?.FileName,
                PageNumber = chunk.Metadata?.PageNumber,
                SourceType = chunk.Metadata?.SourceType
            }
        }).ToList();

        await context.DocumentChunks.AddRangeAsync(entities, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteChunksAsync(Guid documentId, CancellationToken ct = default)
    {
        await context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);
    }
}
