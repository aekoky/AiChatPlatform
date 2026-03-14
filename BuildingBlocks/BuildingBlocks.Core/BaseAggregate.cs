namespace BuildingBlocks.Core;

public abstract class BaseAggregate : BaseEntity
{
    public long Version { get; set; }

    [NonSerialized]
    private readonly Queue<BaseEvent> _uncommittedEvents = new();

    public BaseEvent[] PeekUncommittedEvents() => _uncommittedEvents.ToArray();

    public BaseEvent[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = _uncommittedEvents.ToArray();

        _uncommittedEvents.Clear();

        return dequeuedEvents;
    }

    public void Enqueue(BaseEvent @event) => _uncommittedEvents.Enqueue(@event);

    protected void ApplyAndEnqueue(BaseEvent @event, Action<BaseEvent> applyAction)
    {
        applyAction(@event);
        Enqueue(@event);
    }
}
