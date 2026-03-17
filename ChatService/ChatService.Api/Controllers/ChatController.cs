using ChatService.Api.Extensions;
using ChatService.Application.Dtos;
using ChatService.Application.Features.CloseConversation;
using ChatService.Application.Features.GetConversation;
using ChatService.Application.Features.GetMessages;
using ChatService.Application.Features.ListUserConversations;
using ChatService.Application.Features.SendMessage;
using ChatService.Application.Features.StartChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace ChatService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController(IMessageBus messageBus) : ControllerBase
{
    private readonly IMessageBus _messageBus = messageBus;

    /// <summary>
    /// Starts a new chat session for a user.
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartChat([FromBody] StartChatRequest request)
    {
        var sessionId = Guid.NewGuid();

        var command = new StartChatCommand(sessionId, User.GetUserId(), request.Title);
        await _messageBus.InvokeAsync(command);
        return Accepted(new { Id = sessionId });
    }

    /// <summary>
    /// Sends a message to an active chat session.
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var command = new SendMessageCommand(Guid.NewGuid(), request.SessionId, User.GetUserId(), request.Content);
        await _messageBus.InvokeAsync(command);
        return Accepted();
    }

    /// <summary>
    /// Closes a conversation.
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseConversation([FromBody] CloseConversationRequest request)
    {
        var command = new CloseConversationCommand(request.SessionId, request.Version, User.GetUserId());
        await _messageBus.InvokeAsync(command);
        return Accepted();
    }

    /// <summary>
    /// Retrieves a conversation's details by session ID.
    /// </summary>
    [HttpGet("conversation/{sessionId}")]
    public async Task<IActionResult> GetConversation(Guid sessionId)
    {
        var query = new GetConversationQuery(sessionId, User.GetUserId());
        var result = await _messageBus.InvokeAsync<ConversationDto?>(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all messages for a specific conversation session.
    /// </summary>
    [HttpGet("conversation/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId)
    {
        var query = new GetMessagesQuery(sessionId, User.GetUserId());
        var result = await _messageBus.InvokeAsync<IReadOnlyList<MessageDto>>(query);
        return Ok(result);
    }

    /// <summary>
    /// Lists all conversations associated with a specific user.
    /// </summary>
    [HttpGet("user/conversations")]
    public async Task<IActionResult> ListUserConversations()
    {
        var query = new ListUserConversationsQuery(User.GetUserId());
        var result = await _messageBus.InvokeAsync<IReadOnlyList<ConversationDto>>(query);
        return Ok(result);
    }

    public record StartChatRequest(string Title);
    public record SendMessageRequest(Guid SessionId, string Content);
    public record CloseConversationRequest(Guid SessionId, long Version);
}
