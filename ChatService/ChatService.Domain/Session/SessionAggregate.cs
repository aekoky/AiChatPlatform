using BuildingBlocks.Core;
using ChatService.Domain.Session.Events;

namespace ChatService.Domain.Session;

public class SessionAggregate : BaseAggregate
{
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTime StartedAt { get; private set; }

    public DateTime LastActivityAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public SessionAggregate()
    {
    }

    public static SessionAggregate Create(Guid id, Guid userId, string title)
    {
        if (id == Guid.Empty) throw new DomainException("Session id cannot be empty.");
        if (userId == Guid.Empty) throw new DomainException("User id cannot be empty.");

        var aggregate = new SessionAggregate();

        var @event = SessionCreatedEvent.Create(id, userId, title);

        aggregate.ApplyAndEnqueue(@event, e => aggregate.Apply((SessionCreatedEvent)e));

        return aggregate;
    }

    public void UpdateActivity(DateTime lastActivityAt)
    {
        if (lastActivityAt == default) throw new DomainException("Last activity time is required.");

        var @event = new SessionUpdatedEvent(Id, lastActivityAt);

        ApplyAndEnqueue(@event, e => Apply((SessionUpdatedEvent)e));
    }

    public void Delete()
    {
        var @event = new SessionDeletedEvent(Id);

        ApplyAndEnqueue(@event, e => Apply((SessionDeletedEvent)e));
    }

    private void Apply(SessionCreatedEvent @event)
    {
        Id = @event.Id;
        UserId = @event.UserId;
        Title = @event.Title;
        StartedAt = @event.StartedAt;
        LastActivityAt = @event.LastActivityAt;

        Version++;
    }

    private void Apply(SessionUpdatedEvent @event)
    {
        LastActivityAt = @event.LastActivityAt;

        Version++;
    }

    private void Apply(SessionDeletedEvent @event)
    {
        LastActivityAt = DateTime.UtcNow;
        DeletedAt = DateTime.UtcNow;

        Version++;
    }
}
