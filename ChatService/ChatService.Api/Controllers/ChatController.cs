using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using ChatService.Application.Features.StartChat;
using ChatService.Application.Features.SendMessage;
using ChatService.Application.Features.GetConversation;
using ChatService.Application.Features.ListUserConversations;
using ChatService.Application.Features.CloseConversation;

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
        var command = new StartChatCommand(sessionId, request.UserId);
        await _messageBus.InvokeAsync(command);
        return Accepted(new { Id = sessionId });
    }

    /// <summary>
    /// Sends a message to an active chat session.
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var command = new SendMessageCommand(Guid.NewGuid(), request.SessionId, request.SenderId, request.Content);
        await _messageBus.InvokeAsync(command);
        return Accepted();
    }

    /// <summary>
    /// Closes a conversation.
    /// </summary>
    [HttpPost("close")]
    public async Task<IActionResult> CloseConversation([FromBody] CloseConversationRequest request)
    {
        var command = new CloseConversationCommand(request.SessionId);
        await _messageBus.InvokeAsync(command);
        return Accepted();
    }

    /// <summary>
    /// Retrieves a conversation's details by session ID.
    /// </summary>
    [HttpGet("conversation/{sessionId}")]
    public async Task<IActionResult> GetConversation(Guid sessionId)
    {
        var query = new GetConversationQuery(sessionId);
        var result = await _messageBus.InvokeAsync<ChatService.Application.Dtos.ConversationDto?>(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all messages for a specific conversation session.
    /// </summary>
    [HttpGet("conversation/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId)
    {
        var query = new ChatService.Application.Features.GetMessages.GetMessagesQuery(sessionId);
        var result = await _messageBus.InvokeAsync<IReadOnlyList<ChatService.Application.Dtos.MessageDto>>(query);
        return Ok(result);
    }

    /// <summary>
    /// Lists all conversations associated with a specific user.
    /// </summary>
    [HttpGet("user/{userId}/conversations")]
    public async Task<IActionResult> ListUserConversations(Guid userId)
    {
        var query = new ListUserConversationsQuery(userId);
        var result = await _messageBus.InvokeAsync<IReadOnlyList<ChatService.Application.Dtos.ConversationDto>>(query);
        return Ok(result);
    }

    public record StartChatRequest(Guid UserId);
    public record SendMessageRequest(Guid SessionId, Guid SenderId, string Content);
    public record CloseConversationRequest(Guid SessionId);
}
