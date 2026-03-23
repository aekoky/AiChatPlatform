using BuildingBlocks.Contracts.DocumentEvents;
using DocumentService.Application.Domain;
using DocumentService.Application.Services;
using DocumentService.Application.ValueObjects;
using Wolverine;

namespace DocumentService.Application.Features.UploadDocument;

public class UploadDocumentHandler(
    IDocumentRepository repository,
    IStorageService storage,
    IHttpClientFactory clientFactory)
{
    public async Task Handle(
        UploadDocumentCommand command,
        IMessageContext context,
        CancellationToken ct)
    {
        Stream fileStream;
        string fileName;
        string contentType;

        if (command.File is not null && command.File.Length > 0)
        {
            fileStream = command.File;
            fileName = command!.FileName ?? string.Empty;
            contentType = command!.ContentType ?? string.Empty;
        }
        else
        {
            var client = clientFactory.CreateClient("UrlDocumentsClient");
            var response = await client.GetAsync(command.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException("Failed to download file from URL.");

            fileStream = await response.Content.ReadAsStreamAsync(ct);
            fileName = Path.GetFileName(new Uri(command.Url!).AbsolutePath);
            contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        }

        await storage.UploadAsync(command.Id.ToString(), fileStream, contentType, ct);

        await repository.CreateAsync(new Document
        {
            Id = command.Id,
            UserId = command.UserId,
            SessionId = command.SessionId,
            Scope = command.Scope,
            FileName = fileName,
            ContentType = contentType,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, ct);

        await context.PublishAsync(new DocumentUploadedEvent(
            DocumentId: command.Id,
            UserId: command.UserId,
            SessionId: command.SessionId,
            Scope: command.Scope,
            FileName: fileName,
            ContentType: contentType));
    }
}