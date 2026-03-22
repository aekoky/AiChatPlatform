namespace DocumentService.Contracts.Events;

public record DocumentDeletedEvent(
    Guid DocumentId,
    Guid UserId);
