using BuildingBlocks.Contracts.Events;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmResponseGaveUpHandler(INotificationService notificationService)
{
    public async Task HandleAsync(LlmResponseGaveUpEvent message, CancellationToken ct)
        => await notificationService.SendGaveUpAsync(
            message.UserId, message.RequestId, message.SessionId, message.Reason, ct);
}
