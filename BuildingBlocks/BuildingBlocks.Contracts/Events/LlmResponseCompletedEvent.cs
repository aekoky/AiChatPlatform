namespace BuildingBlocks.Contracts.Events;

public record LlmResponseCompletedEvent(
    Guid RequestId,
    Guid SessionId,
    string FullResponse
);
