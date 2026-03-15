namespace BuildingBlocks.Contracts.Events;

public record LlmResponseGaveUpEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string Reason);