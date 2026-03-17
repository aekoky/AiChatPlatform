namespace ChatService.Application.Features.CloseConversation;

public record CloseConversationCommand(Guid SessionId, long Version, Guid UserId);
