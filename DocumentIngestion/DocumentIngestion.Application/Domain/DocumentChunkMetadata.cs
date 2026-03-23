namespace DocumentIngestion.Application.Domain;

public class DocumentChunkMetadata
{
    public string? FileName { get; set; }
    public string? SourceType { get; set; }
    public int? PageNumber { get; set; }
}
