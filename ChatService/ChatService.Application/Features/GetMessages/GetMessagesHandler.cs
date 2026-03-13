using BuildingBlocks.Core;
using ChatService.Application.Dtos;

namespace ChatService.Application.Features.GetMessages;

public static class GetMessagesHandler
{
    public static async Task<IReadOnlyList<MessageDto>> Handle(
        GetMessagesQuery query,
        IReadOnlyEventStore session,
        CancellationToken ct)
    {
        return await session.QueryListAsync<MessageDto>(
            q => q.Where(m => m.SessionId == query.SessionId).OrderBy(m => m.SentAt),
            ct).ConfigureAwait(false);
    }
}
