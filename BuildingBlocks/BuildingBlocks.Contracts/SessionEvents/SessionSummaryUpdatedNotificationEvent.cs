namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionSummaryUpdatedNotificationEvent(Guid RequestId, Guid SessionId, Guid UserId, string NewSummary);
