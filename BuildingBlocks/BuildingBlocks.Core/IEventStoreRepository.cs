namespace BuildingBlocks.Core;

public interface IEventStoreRepository<TA> where TA : BaseAggregate
{
    Task<TA?> LoadAsync(Guid streamId, long expectedVersion, CancellationToken cancellationToken = default);

    void Save(TA aggregate);
}