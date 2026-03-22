namespace DocumentService.Contracts.Events;

public record DocumentIndexingFailedEvent(
    Guid DocumentId,
    Guid UserId,
    string Reason);
