namespace BuildingBlocks.Core;

public interface IEventStoreRepository<TA> where TA : BaseAggregate
{
    Task<TA?> LoadAsync(Guid streamId, CancellationToken cancellationToken = default);

    Task SaveAsync(TA aggregate, long expectedVersion, CancellationToken cancellationToken = default);
}