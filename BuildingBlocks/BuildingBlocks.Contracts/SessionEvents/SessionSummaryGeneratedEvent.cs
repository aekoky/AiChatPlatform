namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionSummaryGeneratedEvent(
    Guid RequestId,
    Guid SessionId,
    Guid UserId,
    string Summary);
