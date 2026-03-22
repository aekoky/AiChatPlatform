namespace DocumentService.Contracts.Events;

public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid UserId,
    Guid? SessionId,
    string Scope,
    string FileName,
    string ContentType);
