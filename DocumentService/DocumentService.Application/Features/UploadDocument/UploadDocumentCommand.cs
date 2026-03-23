namespace DocumentService.Application.Features.UploadDocument;

public record UploadDocumentCommand(
    Guid Id,
    Guid UserId,
    Guid? SessionId,
    string Scope,
    Stream? File,
    string? FileName,
    string? ContentType,
    string? Url);