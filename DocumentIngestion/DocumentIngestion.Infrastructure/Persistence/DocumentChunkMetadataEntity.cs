namespace DocumentIngestion.Infrastructure.Persistence;

public class DocumentChunkMetadataEntity
{
    public string? FileName { get; set; }
    public string? SourceType { get; set; }
    public int? PageNumber { get; set; }
}
