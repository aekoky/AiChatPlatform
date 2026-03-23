using BuildingBlocks.Core;
using ChatService.Domain.Session;

namespace ChatService.Application.Features.StartChat;

public static class StartChatHandler
{
    public static IEnumerable<object> Handle(
        StartChatCommand command,
        IEventStoreRepository<SessionAggregate> repository)
    {
        var aggregate = SessionAggregate.Create(command.Id, command.UserId, command.Title);

        repository.Save(aggregate);

        return aggregate.DequeueUncommittedEvents();
    }
}
