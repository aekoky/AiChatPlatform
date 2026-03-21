namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionSummaryUpdatedNotificationEvent(Guid SessionId, Guid UserId, string NewSummary);
