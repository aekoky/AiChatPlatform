using BuildingBlocks.Contracts.SessionEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Handlers;

public class SessionSummaryUpdatedNotificationHandler(INotificationService notifications)
{
    public async Task HandleAsync(SessionSummaryUpdatedNotificationEvent message, CancellationToken ct)
    {
        await notifications.SendSummaryUpdatedAsync(
            message.UserId,
            message.SessionId,
            message.NewSummary,
            ct);
    }
}
