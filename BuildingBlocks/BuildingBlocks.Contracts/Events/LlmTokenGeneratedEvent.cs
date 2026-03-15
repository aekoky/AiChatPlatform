namespace BuildingBlocks.Contracts.Events;

public record LlmTokenGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string Token
);
