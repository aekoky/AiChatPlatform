namespace BuildingBlocks.Core;

public interface IReadOnlyEventStore
{
    IQueryable<TProjection> Query<TProjection>() where TProjection : notnull;

    Task<IReadOnlyList<TProjection>> QueryListAsync<TProjection>(
        Func<IQueryable<TProjection>, IQueryable<TProjection>> query,
        CancellationToken ct = default) where TProjection : notnull;

    Task<TProjection?> QueryFirstOrDefaultAsync<TProjection>(
        Func<IQueryable<TProjection>, IQueryable<TProjection>> query,
        CancellationToken ct = default) where TProjection : notnull;
}
