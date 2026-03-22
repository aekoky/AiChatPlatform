namespace BuildingBlocks.Contracts.DocumentEvents;

public record DocumentIndexingFailedEvent(
    Guid DocumentId,
    Guid UserId,
    string Reason);
