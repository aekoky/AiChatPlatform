namespace BuildingBlocks.Core;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime? CreatedAt { get; private set; }
}
