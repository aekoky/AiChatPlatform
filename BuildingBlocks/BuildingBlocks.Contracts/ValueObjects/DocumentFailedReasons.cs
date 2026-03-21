namespace BuildingBlocks.Contracts.ValueObjects;

public static class DocumentFailedReasons
{
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string ParsingFailed = "PARSING_FAILED";
    public const string StorageError = "STORAGE_ERROR";
    public const string EmbeddingFailed = "EMBEDDING_FAILED";
    public const string Unknown = "UNKNOWN";
}