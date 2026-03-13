using BuildingBlocks.Core;
using Marten;

namespace ChatService.Infrastructure.EventStore;

public class MartenReadOnlyEventStore(IQuerySession session) : IReadOnlyEventStore
{
    private readonly IQuerySession _session = session;

    public IQueryable<TProjection> Query<TProjection>() where TProjection : notnull
    {
        return _session.Query<TProjection>();
    }

    public async Task<IReadOnlyList<TProjection>> QueryListAsync<TProjection>(
        Func<IQueryable<TProjection>, IQueryable<TProjection>> query,
        CancellationToken ct = default) where TProjection : notnull
    {
        return await query(_session.Query<TProjection>()).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<TProjection?> QueryFirstOrDefaultAsync<TProjection>(
        Func<IQueryable<TProjection>, IQueryable<TProjection>> query,
        CancellationToken ct = default) where TProjection : notnull
    {
        return await query(_session.Query<TProjection>()).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }
}
