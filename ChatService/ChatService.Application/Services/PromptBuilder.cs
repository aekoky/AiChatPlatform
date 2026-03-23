using BuildingBlocks.Contracts.Models;
using BuildingBlocks.Core;
using ChatService.Application.Dtos;
using ChatService.Domain.ValueObjects;
using Marten;

namespace ChatService.Application.Services;

public class PromptBuilder(IReadOnlyEventStore readOnlyEventStore) : IPromptBuilder
{
    public async Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var sessionDto = await readOnlyEventStore.Query<ConversationDto>()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        var messages = await readOnlyEventStore.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SentAt)
            .Take(20)
            .ToListAsync(ct);

        var history = messages.Select(msg => new ChatTurn(
            msg.Role == MessageRole.User ? "user" : "assistant",
            msg.Content)).ToList();

        if (sessionDto != null && !string.IsNullOrWhiteSpace(sessionDto.Summary))
        {
            history.Add(new ChatTurn("system", $"\n\nConversation summary:\n{sessionDto.Summary}"));
        }

        history.Reverse();

        return history;
    }
}
