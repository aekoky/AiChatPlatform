using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Api;


[Authorize]
public class ChatHub : Hub
{
    // Called by LlmTokenGeneratedHandler — pushes one token to the specific user
    public async Task SendTokenAsync(
        string userId,
        Guid requestId,
        Guid sessionId,
        string token,
        CancellationToken ct)
        => await Clients.User(userId)
            .SendAsync("ReceiveToken", new { requestId, sessionId, token }, ct);

    // Called by LlmResponseCompletedHandler — signals end of stream to the specific user
    public async Task SendCompletedAsync(
        string userId,
        Guid requestId,
        Guid sessionId,
        CancellationToken ct)
        => await Clients.User(userId)
            .SendAsync("ReceiveCompleted", new { requestId, sessionId }, ct);
}