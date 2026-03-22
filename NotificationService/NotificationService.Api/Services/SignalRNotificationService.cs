using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.Services;

namespace NotificationService.Api.Services;

public class SignalRNotificationService(IHubContext<ChatHub> hubContext) : INotificationService
{
    public async Task SendTokenAsync(
        Guid userId, Guid requestId, Guid sessionId, string token, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveToken", new { requestId, sessionId, token }, ct);

    public async Task SendCompletedAsync(
        Guid userId, Guid requestId, Guid sessionId, IReadOnlyList<string>? sources, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveCompleted", new { requestId, sessionId, sources }, ct);

    public async Task SendGaveUpAsync(Guid userId, Guid requestId, Guid sessionId, string reason, CancellationToken ct)
    => await hubContext.Clients
        .User(userId.ToString())
        .SendAsync("ReceiveGaveUp", new { requestId, sessionId, reason }, ct);

    public async Task SendTitleUpdatedAsync(Guid userId, Guid sessionId, string newTitle, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveTitleUpdated", new { sessionId, newTitle }, ct);

    public async Task SendSummaryUpdatedAsync(Guid userId, Guid sessionId, string newSummary, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveSummaryUpdated", new { sessionId, newSummary }, ct);
}