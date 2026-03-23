using DocumentService.Application.Domain;
using DocumentService.Application.Dtos;
using DocumentService.Application.Services;
using DocumentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Infrastructure.Repositories;

public class DocumentRepository(DocumentDbContext context) : IDocumentRepository
{
    public async Task CreateAsync(Document document, CancellationToken ct = default)
    {
        await context.Documents.AddAsync(document, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Document?> GetInternalAsync(Guid id, CancellationToken ct = default)
        => await context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Document?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await context.Documents
            .AsNoTracking()
            .Where(d => d.Id == id && d.UserId == userId)
            .Select(d => new Document
            {
                Id = d.Id,
                UserId = d.UserId,
                SessionId = d.SessionId,
                Scope = d.Scope,
                FileName = d.FileName,
                ContentType = d.ContentType,
                Status = d.Status,
                ChunkCount = d.ChunkCount,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

    public async Task<DocumentStatusDto?> GetStatusAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await context.Documents
            .AsNoTracking()
            .Where(d => d.Id == id && d.UserId == userId)
            .Select(d => new DocumentStatusDto
            {
                Id = d.Id,
                Status = d.Status,
                ChunkCount = d.ChunkCount,
                ErrorMessage = d.ErrorMessage,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Document>> ListAsync(
        Guid userId,
        string? scope,
        Guid? sessionId,
        CancellationToken ct = default)
    {
        var query = context.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId);

        if (scope is not null)
            query = query.Where(d => d.Scope == scope);

        if (sessionId is not null)
            query = query.Where(d => d.SessionId == sessionId);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new Document
            {
                Id = d.Id,
                UserId = d.UserId,
                SessionId = d.SessionId,
                Scope = d.Scope,
                FileName = d.FileName,
                ContentType = d.ContentType,
                Status = d.Status,
                ChunkCount = d.ChunkCount,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        string status,
        int? chunkCount,
        string? errorMessage,
        CancellationToken ct = default)
    {
        await context.Documents
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, status)
                .SetProperty(d => d.ChunkCount, chunkCount)
                .SetProperty(d => d.ErrorMessage, errorMessage)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await context.Documents
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(ct);
    }
}