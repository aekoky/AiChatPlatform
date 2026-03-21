using DocumentService.Application.Domain;
using DocumentService.Application.Services;
using DocumentService.Application.ValueObjects;
using DocumentService.Contracts.Events;
using Wolverine;

namespace DocumentService.Application.Features.UploadDocument;

public class UploadDocumentHandler(
    IDocumentRepository repository,
    IStorageService storage)
{
    public async Task Handle(
        UploadDocumentCommand command,
        IMessageContext context,
        CancellationToken ct)
    {
        await repository.CreateAsync(new Document
        {
            Id = command.Id,
            UserId = command.UserId,
            SessionId = command.SessionId,
            Scope = command.Scope,
            FileName = command.FileName,
            ContentType = command.ContentType,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, ct);
        await storage.UploadAsync(command.Id.ToString(), command.FileStream, command.ContentType, ct);

        await context.PublishAsync(new DocumentUploadedEvent(
            DocumentId: command.Id,
            UserId: command.UserId,
            SessionId: command.SessionId,
            Scope: command.Scope,
            FileName: command.FileName,
            ContentType: command.ContentType));
    }
}