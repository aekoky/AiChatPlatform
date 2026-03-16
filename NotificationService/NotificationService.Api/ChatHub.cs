using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Api;


[Authorize]
public class ChatHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"ConnectionId: {Context.ConnectionId}");
        Console.WriteLine($"UserIdentifier: {Context.UserIdentifier}");
        Console.WriteLine($"Claims: {string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? [])}");
        return base.OnConnectedAsync();
    }
}