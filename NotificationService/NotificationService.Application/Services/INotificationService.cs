namespace NotificationService.Application.Services;

public interface INotificationService
{
    Task SendTokenAsync(Guid userId, Guid requestId, Guid sessionId, string token, CancellationToken ct = default);

    Task SendCompletedAsync(Guid userId, Guid requestId, Guid sessionId, IReadOnlyList<string>? sources, CancellationToken ct = default);

    Task SendGaveUpAsync(Guid userId, Guid requestId, Guid sessionId, string reason, CancellationToken ct = default);

    Task SendTitleUpdatedAsync(Guid userId, Guid sessionId, string newTitle, CancellationToken ct = default);
}