using ChatService.Application.Dtos;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;
using Marten.Events.Projections;

namespace ChatService.Infrastructure.Projections;

public class ConversationProjection : MultiStreamProjection<ConversationDto, Guid>
{
    public ConversationProjection()
    {
        CreateEvent<SessionCreatedEvent>(e => ConversationProjectionLogic.Handle(e)!);
        ProjectEvent<MessageCreatedEvent>((c, e) => ConversationProjectionLogic.Handle(e, c)!);
        ProjectEvent<SessionDeletedEvent>((c, e) => ConversationProjectionLogic.Handle(c)!);

        Identity<SessionCreatedEvent>(x => x.Id);
        Identity<MessageCreatedEvent>(x => x.SessionId);
        Identity<SessionDeletedEvent>(x => x.Id);
    }
}
