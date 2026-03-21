using DocumentIngestion.Application.Domain;

namespace DocumentIngestion.Application.Services;

public interface IVectorStoreRepository
{
    Task SaveChunksAsync(
        List<DocumentChunk> chunks,
        Guid? userId,
        Guid? sessionId,
        string scope,
        CancellationToken ct = default);

    Task DeleteChunksAsync(Guid documentId, CancellationToken ct = default);
}
