using BuildingBlocks.Core;

namespace ChatService.Domain.Session.Events;

public record SessionUpdatedEvent(Guid Id, DateTime LastActivityAt) : BaseEvent;
