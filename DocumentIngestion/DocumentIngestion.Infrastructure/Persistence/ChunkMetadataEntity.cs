namespace DocumentIngestion.Infrastructure.Persistence;

public class ChunkMetadataEntity
{
    public string? FileName { get; set; }
    public string? SourceType { get; set; }
    public int? PageNumber { get; set; }
}
