using DocumentService.Application.Domain;
using DocumentService.Application.Dtos;

namespace DocumentService.Application.Services;

public interface IDocumentRepository
{
    Task CreateAsync(Document document, CancellationToken ct = default);
    Task<Document?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<DocumentStatusDto?> GetStatusAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> ListAsync(Guid userId, string? scope, Guid? sessionId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, int? chunkCount, string? errorMessage, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
