using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmResponseCompletedHandler(
    INotificationService notificationService,
    IStreamBufferService streamBuffer)
{
    public async Task HandleAsync(
        LlmResponseCompletedEvent message,
        CancellationToken ct)
    {
        // Register expected token count
        // Preserves tokens already delivered if they arrived before this event
        streamBuffer.Expect(message.RequestId, message.TokenCount);

        // If all tokens already delivered — send ReceiveCompleted immediately
        if (streamBuffer.IsComplete(message.RequestId))
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