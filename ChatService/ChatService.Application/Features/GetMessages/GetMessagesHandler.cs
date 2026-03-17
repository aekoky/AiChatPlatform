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
        var conversation = await session.QueryFirstOrDefaultAsync<ConversationDto>(
            q => q.Where(c => c.Id == query.SessionId),
            ct).ConfigureAwait(false);

        if (conversation is not null && conversation.UserId != query.UserId)
        {
            throw new DomainException("Forbidden");
        }

        return await session.QueryListAsync<MessageDto>(
            q => q.Where(m => m.SessionId == query.SessionId).OrderBy(m => m.SentAt),
            ct).ConfigureAwait(false);
    }
}
