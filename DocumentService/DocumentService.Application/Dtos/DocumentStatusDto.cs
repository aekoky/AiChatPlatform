namespace DocumentService.Application.Dtos;

public record DocumentStatusDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? ChunkCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime UpdatedAt { get; init; }
}
