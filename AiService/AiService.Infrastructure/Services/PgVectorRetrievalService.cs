using AiService.Application.Services;
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
    public async Task<IReadOnlyList<string>> RetrieveAsync(
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
            .OrderBy(c => c.Embedding!.CosineDistance(vector))
            .Take(topK)
            .Select(c => c.Content)
            .ToListAsync(ct);
    }
}
