namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmResponseCompletedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string FullResponse,
    int TokenCount,
    IReadOnlyList<string>? Sources = null
);
