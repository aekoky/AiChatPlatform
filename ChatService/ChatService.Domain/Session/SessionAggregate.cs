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

    public string Summary { get; private set; } = string.Empty;

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

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title cannot be empty.");

        // For simplicity, we'll use a new event or just reuse SessionCreated's property logic
        // But a specific event is cleaner for Marten
        var @event = new SessionTitleUpdatedEvent(Id, title);

        ApplyAndEnqueue(@event, e => Apply((SessionTitleUpdatedEvent)e));
    }

    public void Delete()
    {
        var @event = new SessionDeletedEvent(Id);

        ApplyAndEnqueue(@event, e => Apply((SessionDeletedEvent)e));
    }

    public void UpdateSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) throw new DomainException("Summary cannot be empty.");

        var @event = new SessionSummaryUpdatedEvent(Id, summary);

        ApplyAndEnqueue(@event, e => Apply((SessionSummaryUpdatedEvent)e));
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

    private void Apply(SessionSummaryUpdatedEvent @event)
    {
        Summary = @event.Summary;
        LastActivityAt = @event.UpdatedAt;

        Version++;
    }

    private void Apply(SessionTitleUpdatedEvent @event)
    {
        Title = @event.Title;
        LastActivityAt = DateTime.UtcNow;

        Version++;
    }
}
