namespace BuildingBlocks.Contracts.ValueObjects;

public static class GaveUpReasons
{
    public const string LlmError = "LLM_ERROR";
    public const string Timeout = "LLM_TIMEOUT";
    public const string MaxRetriesExceeded = "MAX_RETRIES_EXCEEDED";
    public const string SessionDeleted = "SESSION_DELETED";
}