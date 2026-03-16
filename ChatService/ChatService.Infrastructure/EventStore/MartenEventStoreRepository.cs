using BuildingBlocks.Core;
using Marten;

namespace ChatService.Infrastructure.EventStore;

public class MartenEventStoreRepository<TA>(IDocumentSession session) : IEventStoreRepository<TA> where TA : BaseAggregate, new()
{
    public async Task<TA?> LoadAsync(Guid streamId, long expectedVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventStream = await session.Events.FetchForWriting<TA>(streamId, cancellationToken).ConfigureAwait(false);
            var aggregate = eventStream.Aggregate;

            aggregate?.Version = eventStream.CurrentVersion ?? 0;

            return aggregate;
        }
        catch
        {
            return default;
        }
    }

    public void Save(TA aggregate)
    {
        var events = aggregate.PeekUncommittedEvents();

        if (events is not { Length: > 0 })
            return;

        session.Events.Append(aggregate.Id, aggregate.Version, [.. events]);
    }
}
