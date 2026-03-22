using BuildingBlocks.Contracts.SessionEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class SessionTitleUpdatedNotificationHandler(INotificationService notifications)
{
    public async Task Handle(SessionTitleUpdatedNotificationEvent message, CancellationToken ct)
        => await notifications.SendTitleUpdatedAsync(message.UserId, message.SessionId, message.NewTitle, ct);
}
