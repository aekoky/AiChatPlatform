namespace BuildingBlocks.Contracts.SessionEvents;

public record SessionTitleUpdatedNotificationEvent(Guid RequestId, Guid SessionId, Guid UserId, string NewTitle);
