namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmResponseGaveUpEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string Reason);