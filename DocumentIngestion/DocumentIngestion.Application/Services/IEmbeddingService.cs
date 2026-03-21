namespace DocumentIngestion.Application.Services;

public interface IEmbeddingService
{
    Task<List<float[]>> EmbedAsync(List<string> texts, CancellationToken ct = default);
}
