using DocumentService.Application.Services;
using DocumentService.Contracts.Events;
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

        await storage.DeleteAsync(document.Id.ToString(), ct);
        await repository.DeleteAsync(command.Id, ct);

        await context.PublishAsync(new DocumentDeletedEvent(
            DocumentId: command.Id,
            UserId: command.UserId));
    }
}