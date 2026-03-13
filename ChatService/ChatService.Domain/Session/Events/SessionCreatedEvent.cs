using BuildingBlocks.Core;

namespace ChatService.Domain.Session.Events;

public record SessionCreatedEvent : BaseEvent
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime LastActivityAt { get; init; }

    public static SessionCreatedEvent Create(Guid id, Guid userId)
    {
        if (id == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (userId == Guid.Empty) throw new DomainException("User id cannot be empty.");

        var now = DateTime.UtcNow;

        return new SessionCreatedEvent
        {
            Id = id,
            UserId = userId,
            StartedAt = now,
            LastActivityAt = now
        };
    }
}
