namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionTitleUpdatedNotificationEvent(Guid SessionId, Guid UserId, string NewTitle);
