using AiService.Application.Dtos;
using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AiService.Infrastructure.Services;

public class PgVectorRetrievalService(
    AiDbContext context,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : IRagRetrievalService
{
    public async Task<IReadOnlyList<DocumentChunkDto>> RetrieveAsync(
        string query,
        Guid userId,
        Guid? sessionId,
        int topK = 5,
        CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateVectorAsync(query, cancellationToken: ct);
        var vector = new Vector(embedding);

        return await context.DocumentChunks
            .AsNoTracking()
            .Where(c =>
                c.Scope == "global" ||
                (c.Scope == "user" && c.UserId == userId) ||
                (c.Scope == "session" && c.SessionId == sessionId))
            .Select(c => new
            {
                Chunk = c,
                Distance = c.Embedding!.CosineDistance(vector)
            })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Select(x => new DocumentChunkDto
            {
                Content = x.Chunk.Content,
                FileName = x.Chunk.Metadata != null ? x.Chunk.Metadata.FileName : null,
                SourceType = x.Chunk.Metadata != null ? x.Chunk.Metadata.SourceType : null,
                PageNumber = x.Chunk.Metadata != null ? x.Chunk.Metadata.PageNumber : null,
                ChunkIndex = x.Chunk.ChunkIndex,
                RelevanceScore = 1.0 - x.Distance
            })
            .ToListAsync(ct);
    }
}
