using BuildingBlocks.Core;

namespace ChatService.Domain.Session.Events;

public record SessionSummaryUpdatedEvent(Guid Id, string Summary) : BaseEvent
{
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
