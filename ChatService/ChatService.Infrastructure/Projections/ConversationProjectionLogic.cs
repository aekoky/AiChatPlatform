using ChatService.Application.Dtos;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;

namespace ChatService.Infrastructure.Projections;

public static class ConversationProjectionLogic
{
    public static ConversationDto? Handle(SessionCreatedEvent @event)
    {
        return new ConversationDto
        {
            Id = @event.Id,
            UserId = @event.UserId,
            StartedAt = @event.StartedAt,
            LastActivityAt = @event.LastActivityAt
        };
    }

    public static ConversationDto? Handle(MessageCreatedEvent @event, ConversationDto? current)
    {
        if (current is null) return null;

        return current with
        {
            LastActivityAt = @event.SentAt
        };
    }

    public static ConversationDto? Handle(ConversationDto? current)
    {
        if (current is null) return null;

        return current with
        {
            LastActivityAt = DateTime.UtcNow
        };
    }
}