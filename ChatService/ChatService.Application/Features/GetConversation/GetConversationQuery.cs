namespace ChatService.Application.Features.GetConversation;

public record GetConversationQuery(Guid SessionId, Guid UserId);
