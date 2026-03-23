using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Handlers;

public class LlmTokensGeneratedHandler(
    INotificationService notificationService)
{
    public async Task HandleAsync(
        LlmTokensGeneratedEvent message,
        CancellationToken ct)
    {
        await notificationService.SendTokenAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            message.Token,
            ct);
    }
}