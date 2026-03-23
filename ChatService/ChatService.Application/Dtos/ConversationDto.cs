namespace ChatService.Application.Dtos;

public record ConversationDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime LastActivityAt { get; init; }

    public bool Closed { get; init; }

    public long Version { get; init; }
}
