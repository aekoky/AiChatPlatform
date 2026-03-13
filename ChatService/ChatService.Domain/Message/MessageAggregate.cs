using BuildingBlocks.Core;
using ChatService.Domain.Message.Events;

namespace ChatService.Domain.Message;

public class MessageAggregate : BaseAggregate
{
    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAt { get; private set; }

    public MessageAggregate()
    {
    }

    public static MessageAggregate Create(Guid id, Guid sessionId, Guid senderId, string content)
    {
        if (id == Guid.Empty) throw new DomainException("Message id cannot be empty.");
        if (senderId == Guid.Empty) throw new DomainException("Sender id cannot be empty.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content cannot be empty.");

        var message = new MessageAggregate();

        var @event = MessageCreatedEvent.Create(id, sessionId, senderId, content);

        message.ApplyAndEnqueue(@event, e => message.Apply((MessageCreatedEvent)e));

        return message;
    }

    private void Apply(MessageCreatedEvent @event)
    {
        Id = @event.Id;
        SenderId = @event.SenderId;
        Content = @event.Content;
        SentAt = @event.SentAt;

        Version++;
    }
}
