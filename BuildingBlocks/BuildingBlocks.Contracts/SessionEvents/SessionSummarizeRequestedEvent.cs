using BuildingBlocks.Contracts.Models;

namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionSummarizeRequestedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    IReadOnlyList<ChatTurn> Messages);
