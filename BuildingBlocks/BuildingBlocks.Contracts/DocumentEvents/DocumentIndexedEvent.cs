namespace BuildingBlocks.Contracts.DocumentEvents;

public record DocumentIndexedEvent(
    Guid DocumentId,
    Guid UserId,
    int ChunkCount);
