using ChatService.Application.Dtos;
using ChatService.Domain.Message.Events;
using Marten.Events.Projections;

namespace ChatService.Infrastructure.Projections;

public class MessageProjection : EventProjection
{
    public MessageProjection()
    {
        Project<MessageCreatedEvent>((e, operations) =>
        {
            operations.Store(new MessageDto
            {
                Id = e.Id,
                SessionId = e.SessionId,
                SenderId = e.SenderId,
                Content = e.Content,
                Role = e.Role,
                SentAt = e.SentAt
            });
        });
    }
}
