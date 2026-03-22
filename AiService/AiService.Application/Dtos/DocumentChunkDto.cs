namespace AiService.Application.Dtos;

public class DocumentChunkDto
{
    public string Content { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? SourceType { get; set; }
    public int ChunkIndex { get; set; }
    public double RelevanceScore { get; set; }
    public int? PageNumber { get; set; }
}