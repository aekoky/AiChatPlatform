using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Api;


[Authorize]
public class ChatHub : Hub
{
}