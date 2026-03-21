namespace DocumentService.Application.Features.DeleteDocument;

public record DeleteDocumentCommand(Guid Id, Guid UserId);
