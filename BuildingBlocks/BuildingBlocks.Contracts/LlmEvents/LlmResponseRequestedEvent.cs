using BuildingBlocks.Contracts.Models;

namespace BuildingBlocks.Contracts.LlmEvents;

public record LlmResponseRequestedEvent(
     Guid RequestId,
     Guid SessionId,
     Guid UserId,
     IReadOnlyList<ChatTurn> Messages
);
