using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Handlers;

public class LlmSourcesFoundHandler(INotificationService notificationService)
{
    public async Task HandleAsync(
        LlmSourcesFoundEvent message,
        CancellationToken ct)
    {
        await notificationService.SendSourcesAsync(
            message.UserId,
            message.RequestId,
            message.SessionId,
            message.Sources,
            ct);
    }
}
