using DocumentIngestion.Application.Services;
using Microsoft.Extensions.AI;

namespace DocumentIngestion.Infrastructure.Services;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    public async Task<List<float[]>> EmbedAsync(List<string> texts, CancellationToken ct = default)
    {
        var embeddings = await generator.GenerateAsync(texts, cancellationToken: ct);
        return [.. embeddings.Select(e => e.Vector.ToArray())];
    }
}
