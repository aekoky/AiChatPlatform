namespace BuildingBlocks.Core;

public class ConcurrencyException : DomainException
{
    public ConcurrencyException()
        : base()
    {
    }

    public ConcurrencyException(string message)
        : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
