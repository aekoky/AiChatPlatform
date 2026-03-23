namespace BuildingBlocks.Contracts.DocumentEvents;

public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid UserId,
    Guid? SessionId,
    string Scope,
    string FileName,
    string ContentType);
