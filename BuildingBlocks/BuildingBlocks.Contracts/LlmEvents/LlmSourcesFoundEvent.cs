namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmSourcesFoundEvent(
    Guid RequestId, 
    Guid SessionId, 
    Guid UserId, 
    IReadOnlyList<string> Sources
);
