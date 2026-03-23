namespace AiService.Application.Dtos;

public record RagToolResult(
    bool Success,
    string ContextString,
    IReadOnlyList<string> SourceReferences)
{
    public static RagToolResult Empty => new(false, string.Empty, []);
}