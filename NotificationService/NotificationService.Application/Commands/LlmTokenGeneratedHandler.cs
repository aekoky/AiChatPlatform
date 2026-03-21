using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmTokensGeneratedHandler(
    INotificationService notificationService,
    IStreamBufferService streamBuffer)
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

        if (streamBuffer.TokensDelivered(message.RequestId, message.TokenCount))
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