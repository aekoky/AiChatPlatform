namespace ChatService.Application.Dtos;

public record ConversationDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime LastActivityAt { get; init; }
}
