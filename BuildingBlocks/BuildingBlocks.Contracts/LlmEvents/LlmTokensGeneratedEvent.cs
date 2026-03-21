namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmTokensGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string Token,
    int TokenCount
);
