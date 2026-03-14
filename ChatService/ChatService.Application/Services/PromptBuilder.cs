using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatService.Application.Dtos;
using Marten;

namespace ChatService.Application.Services;

public class PromptBuilder(IQuerySession session) : IPromptBuilder
{
    public async Task<string> BuildAsync(Guid sessionId, CancellationToken ct)
    {
        var messages = await session.Query<MessageDto>()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .Take(20) // Limit to last 20 messages for context
            .ToListAsync(ct);

        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            sb.AppendLine($"{msg.Role.ToString().ToLower()}: {msg.Content}");
        }

        return sb.ToString();
    }
}
