namespace ChatService.Application.Features.StartChat;

public record StartChatCommand(Guid Id, Guid UserId, string Title);
