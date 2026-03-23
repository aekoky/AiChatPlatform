using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Handlers;

public class LlmResponseRetryingHandler(INotificationService notifications)
{
    public async Task HandleAsync(LlmResponseRetryingEvent message, CancellationToken ct)
    {
        await notifications.SendRetryingAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            ct);
    }
}
