using BuildingBlocks.Core;
using ChatService.Application.Dtos;

namespace ChatService.Application.Features.GetConversation;

public static class GetConversationHandler
{
    public static async Task<ConversationDto?> Handle(
        GetConversationQuery query,
        IReadOnlyEventStore session,
        CancellationToken ct)
    {
        return await session.QueryFirstOrDefaultAsync<ConversationDto>(
            q => q.Where(c => c.Id == query.SessionId),
            ct).ConfigureAwait(false);
    }
}
