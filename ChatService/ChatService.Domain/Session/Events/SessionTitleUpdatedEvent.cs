using BuildingBlocks.Core;

namespace ChatService.Domain.Session.Events;

public record SessionTitleUpdatedEvent(Guid Id, string Title) : BaseEvent;
