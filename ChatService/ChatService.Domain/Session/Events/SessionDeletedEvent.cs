using BuildingBlocks.Core;

namespace ChatService.Domain.Session.Events;

public record SessionDeletedEvent(Guid Id) : BaseEvent;
