namespace DocumentService.Application.Dtos;

public record DocumentDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public Guid? SessionId { get; init; }
    public string Scope { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int? ChunkCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}