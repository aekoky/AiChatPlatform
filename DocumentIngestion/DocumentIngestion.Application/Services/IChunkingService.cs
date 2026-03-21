using DocumentIngestion.Application.Domain;

namespace DocumentIngestion.Application.Services;

public interface IChunkingService
{
    Task<List<DocumentChunk>> ParseAndChunkAsync(
        Stream stream,
        string contentType,
        Guid documentId,
        CancellationToken ct = default);
}
