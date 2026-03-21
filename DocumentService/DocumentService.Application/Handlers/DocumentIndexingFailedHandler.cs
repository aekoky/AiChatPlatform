using DocumentService.Application.Services;
using DocumentService.Application.ValueObjects;
using DocumentService.Contracts.Events;

namespace DocumentService.Application.Handlers;

public class DocumentIndexingFailedHandler(IDocumentRepository repository)
{
    public async Task Handle(
        DocumentIndexingFailedEvent message,
        CancellationToken ct)
    {
        await repository.UpdateStatusAsync(
            message.DocumentId,
            DocumentStatus.Failed,
            chunkCount: null,
            message.Reason,
            ct);
    }
}