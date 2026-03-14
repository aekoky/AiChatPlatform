using BuildingBlocks.Core;
using ChatService.Domain.Session;
using Wolverine;

namespace ChatService.Application.Features.StartChat;

public static class StartChatHandler
{
    public static IEnumerable<object> Handle(
        StartChatCommand command,
        IEventStoreRepository<SessionAggregate> repository,
        IMessageContext context)
    {
        var aggregate = SessionAggregate.Create(command.Id, command.UserId);

        repository.Save(aggregate, expectedVersion: 0);

        return aggregate.DequeueUncommittedEvents();
    }
}
