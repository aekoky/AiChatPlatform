namespace BuildingBlocks.Contracts.Events;

public record LlmResponseCompletedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string FullResponse,
    int TokenCount
);
