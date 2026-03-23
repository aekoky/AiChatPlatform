namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmResponseRetryingEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId
);