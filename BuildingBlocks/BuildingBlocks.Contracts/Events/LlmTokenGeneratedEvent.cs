namespace BuildingBlocks.Contracts.Events;

public record LlmTokenGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    string Token
);
