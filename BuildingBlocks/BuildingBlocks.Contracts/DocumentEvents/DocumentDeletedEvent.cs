namespace BuildingBlocks.Contracts.DocumentEvents;

public record DocumentDeletedEvent(
    Guid DocumentId,
    Guid UserId);
