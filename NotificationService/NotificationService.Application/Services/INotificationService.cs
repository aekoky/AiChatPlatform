namespace NotificationService.Application.Services;

public interface INotificationService
{
    Task SendTokenAsync(Guid userId, Guid requestId, Guid sessionId, string token, CancellationToken ct);

    Task SendCompletedAsync(Guid userId, Guid requestId, Guid sessionId, CancellationToken ct);

    Task SendGaveUpAsync(Guid userId, Guid requestId, Guid sessionId, string reason, CancellationToken ct);

    Task SendTitleUpdatedAsync(Guid userId, Guid sessionId, string newTitle, CancellationToken ct);

    Task SendSummaryUpdatedAsync(Guid userId, Guid sessionId, string newSummary, CancellationToken ct);
}