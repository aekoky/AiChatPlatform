namespace BuildingBlocks.Contracts.Events;

public record LlmResponseRetryingEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId
);