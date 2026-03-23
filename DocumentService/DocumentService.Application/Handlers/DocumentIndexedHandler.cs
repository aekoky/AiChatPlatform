using BuildingBlocks.Contracts.DocumentEvents;
using DocumentService.Application.Services;
using DocumentService.Application.ValueObjects;

namespace DocumentService.Application.Handlers;

public class DocumentIndexedHandler(IDocumentRepository repository)
{
    public async Task Handle(
        DocumentIndexedEvent message,
        CancellationToken ct)
    {
        await repository.UpdateStatusAsync(
            message.DocumentId,
            DocumentStatus.Indexed,
            message.ChunkCount,
            errorMessage: null,
            ct);
    }
}
