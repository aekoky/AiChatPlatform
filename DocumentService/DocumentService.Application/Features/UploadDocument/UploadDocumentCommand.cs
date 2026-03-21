namespace DocumentService.Application.Features.UploadDocument;

public record UploadDocumentCommand(
    Guid Id,
    Guid UserId,
    Guid? SessionId,
    string Scope,
    string FileName,
    string ContentType,
    Stream FileStream);