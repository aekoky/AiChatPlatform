using Pgvector;

namespace DocumentIngestion.Application.Domain;

public class DocumentChunkEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}
