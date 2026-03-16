using BuildingBlocks.Contracts.Events;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmTokenGeneratedHandler(
    INotificationService notificationService,
    IStreamBufferService streamBuffer)
{
    public async Task HandleAsync(
        LlmTokenGeneratedEvent message,
        CancellationToken ct)
    {
        // Push token to client immediately
        await notificationService.SendTokenAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            message.Token,
            ct);

        // Atomic — returns true exactly once when last token is delivered
        if (streamBuffer.TokenDelivered(message.RequestId))
        {
            await notificationService.SendCompletedAsync(
                message.UserId,
                message.RequestId,
                message.SessionId,
                ct);

            streamBuffer.Clear(message.RequestId);
        }
    }
}