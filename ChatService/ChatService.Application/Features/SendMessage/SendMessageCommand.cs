namespace ChatService.Application.Features.SendMessage;

public record SendMessageCommand(Guid Id, Guid SessionId, Guid SenderId, string Content);
