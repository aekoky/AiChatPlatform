using AiService.Application.Dtos;

namespace AiService.Application.Services;

public interface IRagRetrievalService
{
    Task<IReadOnlyList<DocumentChunkDto>> RetrieveAsync(
        string query,
        Guid userId,
        Guid? sessionId,
        int topK = 5,
        CancellationToken ct = default);
}
