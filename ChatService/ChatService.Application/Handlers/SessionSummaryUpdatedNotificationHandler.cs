using BuildingBlocks.Contracts.SessionEvents;
namespace ChatService.Application.Handlers;

public class SessionSummaryUpdatedNotificationHandler()
{
    public static async Task<object> Handle(
        SessionSummaryUpdatedNotificationEvent message)
    {
        return new SessionSummaryGeneratedEvent(message.RequestId, message.SessionId, message.UserId, message.NewSummary);
    }
}
