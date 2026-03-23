using BuildingBlocks.Contracts.DocumentEvents;
using DocumentIngestion.Application.Services;

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