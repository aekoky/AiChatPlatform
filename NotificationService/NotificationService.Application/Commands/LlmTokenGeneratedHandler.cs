using BuildingBlocks.Contracts.Events;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmTokenGeneratedHandler(INotificationService notificationService)
{
    public async Task HandleAsync(
        LlmTokenGeneratedEvent message,
        CancellationToken ct)
        => await notificationService.SendTokenAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            message.Token,
            ct);
}