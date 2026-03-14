namespace BuildingBlocks.Contracts.Events;

public record LlmResponseRequestedEvent(
    Guid RequestId,
    Guid SessionId,
    string Prompt
);
