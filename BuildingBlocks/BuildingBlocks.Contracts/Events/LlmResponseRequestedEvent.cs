namespace BuildingBlocks.Contracts.Events;

public record LlmResponseRequestedEvent(
     Guid RequestId,
     Guid SessionId,
     Guid UserId,
     IReadOnlyList<ChatTurn> Messages
);
