using System;

namespace DocumentIngestion.Application.Exceptions;

public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}
