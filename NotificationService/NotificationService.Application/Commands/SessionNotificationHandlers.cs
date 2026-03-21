using BuildingBlocks.Contracts.SessionEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class SessionNotificationHandlers(INotificationService notifications)
{
    public async Task Handle(SessionTitleUpdatedNotificationEvent message, CancellationToken ct)
        => await notifications.SendTitleUpdatedAsync(message.UserId, message.SessionId, message.NewTitle, ct);

    public async Task Handle(SessionSummaryUpdatedNotificationEvent message, CancellationToken ct)
        => await notifications.SendSummaryUpdatedAsync(message.UserId, message.SessionId, message.NewSummary, ct);
}
