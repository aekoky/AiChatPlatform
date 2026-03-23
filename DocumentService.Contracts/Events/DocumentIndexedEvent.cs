namespace DocumentService.Contracts.Events;

public record DocumentIndexedEvent(
    Guid DocumentId,
    Guid UserId,
    int ChunkCount);
