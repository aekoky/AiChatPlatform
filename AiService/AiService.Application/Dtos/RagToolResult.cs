namespace AiService.Application.Dtos;

public record RagToolResult(
    bool Success, 
    string ContextString, 
    IReadOnlyList<string> SourceReferences);
