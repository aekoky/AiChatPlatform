using BuildingBlocks.Core;
using ChatService.Domain.Session;
using System.Collections;
using Wolverine;

namespace ChatService.Application.Features.CloseConversation;

public static class CloseConversationHandler
{
    public static async Task<IEnumerable<object>> Handle(
        CloseConversationCommand command,
        IEventStoreRepository<SessionAggregate> repository,
        CancellationToken ct)
    {
        var aggregate = await repository.LoadAsync(command.SessionId, command.Version, ct).ConfigureAwait(false);

        if (aggregate is null)
        {
            return [];
        }

        aggregate.Delete();

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
