using BuildingBlocks.Contracts.Models;
using ChatService.Application.Dtos;
using ChatService.Domain.ValueObjects;
using Marten;

namespace ChatService.Application.Services;

public class PromptBuilder(IQuerySession session) : IPromptBuilder
{
    public async Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var sessionDto = await session.Query<ConversationDto>()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        var messages = await session.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SentAt)
            .Take(20)
            .ToListAsync(ct);

        var history = messages.Select(msg => new ChatTurn(
            msg.Role == MessageRole.User ? "user" : "assistant",
            msg.Content));

        var systemContent = "You are a helpful assistant.";
        if (sessionDto != null && !string.IsNullOrWhiteSpace(sessionDto.Summary))
        {
            systemContent += $"\n\nBelow is a summary of the conversation context so far:\n{sessionDto.Summary}";
        }

        var result = new List<ChatTurn>
        {
            new("system", systemContent)
        };

        result.AddRange(history);

        return result;
    }
}
