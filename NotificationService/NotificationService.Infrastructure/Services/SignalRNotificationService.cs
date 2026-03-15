using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.Abstractions;

namespace NotificationService.Infrastructure.Services;

public class SignalRNotificationService(IHubContext<ChatHub> hubContext) : INotificationService
{
    public async Task SendTokenAsync(
        Guid userId, Guid requestId, Guid sessionId, string token, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveToken", new { requestId, sessionId, token }, ct);

    public async Task SendCompletedAsync(
        Guid userId, Guid requestId, Guid sessionId, CancellationToken ct)
        => await hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveCompleted", new { requestId, sessionId }, ct);
}