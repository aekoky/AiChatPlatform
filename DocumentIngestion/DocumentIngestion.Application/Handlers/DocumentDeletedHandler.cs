using DocumentIngestion.Application.Services;
using DocumentService.Contracts.Events;

namespace DocumentIngestion.Application.Handlers;

public class DocumentDeletedHandler(IVectorStoreRepository vectorStore)
{
    public async Task Handle(
        DocumentDeletedEvent message,
        CancellationToken ct)
    {
        await vectorStore.DeleteChunksAsync(message.DocumentId, ct);
    }
}