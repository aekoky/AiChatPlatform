using BuildingBlocks.Core;
using ChatService.Domain.Session;

namespace ChatService.Application.Features.CloseConversation;

public static class CloseConversationHandler
{
    public static async Task Handle(
        CloseConversationCommand command,
        IEventStoreRepository<SessionAggregate> repository,
        CancellationToken ct)
    {
        var aggregate = await repository.LoadAsync(command.SessionId, ct).ConfigureAwait(false);

        if (aggregate is null)
        {
            return;
        }

        aggregate.Delete();

        await repository.SaveAsync(aggregate, expectedVersion: aggregate.Version, ct).ConfigureAwait(false);
    }
}
