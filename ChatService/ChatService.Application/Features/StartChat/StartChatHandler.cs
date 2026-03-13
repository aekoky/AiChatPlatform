using BuildingBlocks.Core;
using ChatService.Domain.Session;

namespace ChatService.Application.Features.StartChat;

public static class StartChatHandler
{
    public static async Task Handle(
        StartChatCommand command,
        IEventStoreRepository<SessionAggregate> repository,
        CancellationToken ct)
    {
        var aggregate = SessionAggregate.Create(command.Id, command.UserId);

        await repository.SaveAsync(aggregate, expectedVersion: 0, ct).ConfigureAwait(false);
    }
}
