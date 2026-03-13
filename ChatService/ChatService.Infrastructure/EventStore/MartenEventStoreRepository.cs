using BuildingBlocks.Core;
using JasperFx.Events;
using Marten;

namespace ChatService.Infrastructure.EventStore;

public class MartenEventStoreRepository<TA>(IDocumentSession session) : IEventStoreRepository<TA> where TA : BaseAggregate, new()
{
    private readonly IDocumentSession _session = session;

    public async Task<TA?> LoadAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventStream = await _session.Events.FetchForWriting<TA>(streamId, cancellationToken).ConfigureAwait(false);
            var aggregate = eventStream.Aggregate;

            aggregate?.Version = eventStream.CurrentVersion ?? 0;

            return aggregate;
        }
        catch
        {
            return default;
        }
    }

    public async Task SaveAsync(TA aggregate, long expectedVersion, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DequeueUncommittedEvents();

        if (events is not { Length: > 0 })
        {
            return;
        }

        if (expectedVersion >= 0)
        {
            _session.Events.Append(aggregate.Id, expectedVersion + events.Length, [.. events]);
        }
        else
        {
            _session.Events.Append(aggregate.Id, [.. events]);
        }

        try
        {
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (EventStreamUnexpectedMaxEventIdException ex)
        {
            throw new DomainException("Concurrency conflict while saving aggregate.", ex);
        }

        if (expectedVersion >= 0)
        {
            aggregate.Version = expectedVersion + events.Length;
        }
        else
        {
            // attempt to read stream state
            var state = await _session.Events.FetchStreamStateAsync(aggregate.Id, cancellationToken).ConfigureAwait(false);
            if (state is not null)
            {
                aggregate.Version = state.Version;
            }
            else
            {
                aggregate.Version += events.Length;
            }
        }
    }
}
