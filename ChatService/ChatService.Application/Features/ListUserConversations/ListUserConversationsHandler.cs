using BuildingBlocks.Core;
using ChatService.Application.Dtos;

namespace ChatService.Application.Features.ListUserConversations;

public static class ListUserConversationsHandler
{
    public static async Task<IReadOnlyList<ConversationDto>> Handle(
        ListUserConversationsQuery query,
        IReadOnlyEventStore session,
        CancellationToken ct)
    {
        return await session.QueryListAsync<ConversationDto>(
            q => q.Where(c => c.UserId == query.UserId && !c.Closed),
            ct).ConfigureAwait(false);
    }
}
