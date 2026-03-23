using BuildingBlocks.Contracts.DocumentEvents;
using DocumentService.Application.Services;
using Wolverine;

namespace DocumentService.Application.Features.DeleteDocument;

public class DeleteDocumentHandler(
    IDocumentRepository repository,
    IStorageService storage)
{
    public async Task Handle(
        DeleteDocumentCommand command,
        IMessageContext context,
        CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(command.Id, command.UserId, ct);

        if (document is null)
            return;

        await repository.DeleteAsync(command.Id, ct);
        await storage.DeleteAsync(document.Id.ToString(), ct);

        await context.PublishAsync(new DocumentDeletedEvent(
            DocumentId: command.Id,
            UserId: command.UserId));
    }
}