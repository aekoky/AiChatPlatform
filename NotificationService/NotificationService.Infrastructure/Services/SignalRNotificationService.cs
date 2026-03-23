using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.Services;

namespace NotificationService.Infrastructure.Services;

public class SignalRNotificationService(IHubContext<ChatHub> hubContext) : INotificationService
{
    public async Task SendTokenAsync(
        Guid userId, Guid requestId, Guid sessionId, string token, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveToken", new { requestId, sessionId, token }, ct);

    public async Task SendSourcesAsync(
        Guid userId, Guid requestId, Guid sessionId, IReadOnlyList<string>? sources, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveSources", new { requestId, sessionId, sources = sources ?? [] }, ct);

    public async Task SendCompletedAsync(
        Guid userId, Guid requestId, Guid sessionId, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveCompleted", new { requestId, sessionId }, ct);

    public async Task SendGaveUpAsync(
        Guid userId, Guid requestId, Guid sessionId, string reason, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveGaveUp", new { requestId, sessionId, reason }, ct);
}