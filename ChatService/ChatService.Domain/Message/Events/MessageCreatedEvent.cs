using BuildingBlocks.Core;

namespace ChatService.Domain.Message.Events;

public record MessageCreatedEvent : BaseEvent
{
    public Guid Id { get; init; }

    public DateTime CreatedAt { get; set; }

    public Guid SenderId { get; private set; }

    public Guid SessionId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAt { get; private set; }

    public static MessageCreatedEvent Create(Guid id, Guid sessionId, Guid senderId, string content)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        if (sessionId == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (senderId == Guid.Empty) throw new DomainException("Sender id cannot be empty.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content cannot be empty.");
        return new MessageCreatedEvent
        {
            Id = id,
            SessionId = sessionId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
