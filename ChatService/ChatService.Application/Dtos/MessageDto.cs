using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Dtos;

public record MessageDto
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public Guid SenderId { get; init; }

    public string Content { get; init; } = string.Empty;

    public MessageRole Role { get; init; }

    public DateTime SentAt { get; init; }

    public long Version { get; init; }
}
