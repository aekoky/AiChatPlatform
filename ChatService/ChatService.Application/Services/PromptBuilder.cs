using BuildingBlocks.Contracts.Events;
using ChatService.Domain.ValueObjects;
using ChatService.Application.Dtos;
using Marten;
using System.Collections.Generic;

namespace ChatService.Application.Services;

public class PromptBuilder(IQuerySession session) : IPromptBuilder
{
    public async Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var messages = await session.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .Take(20)
            .ToListAsync(ct);

        var history = messages.Select(msg => new ChatTurn(
            msg.Role == MessageRole.User ? "user" : "assistant",
            msg.Content));

        var result = new List<ChatTurn>
        {
            new("system", "You are a helpful assistant.")
        };
        
        result.AddRange(history);
        
        return result;
    }
}
