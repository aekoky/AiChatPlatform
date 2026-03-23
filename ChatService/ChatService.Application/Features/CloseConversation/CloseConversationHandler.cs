using BuildingBlocks.Core;
using ChatService.Domain.Session;

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

        if (aggregate.UserId != command.UserId)
        {
            throw new DomainException("Forbidden");
        }

        aggregate.Delete();

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
