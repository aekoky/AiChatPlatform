namespace ChatService.Application.Dtos;

public record MessageDto
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public Guid SenderId { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime SentAt { get; init; }
}
