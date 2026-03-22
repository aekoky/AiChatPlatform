using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmResponseCompletedHandler(
    INotificationService notificationService)
{
    public async Task HandleAsync(
        LlmResponseCompletedEvent message,
        CancellationToken ct)
    {
        await notificationService.SendCompletedAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            message.Sources,
            ct);
    }
}