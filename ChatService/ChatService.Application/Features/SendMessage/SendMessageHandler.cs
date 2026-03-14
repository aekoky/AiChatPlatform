using BuildingBlocks.Core;
using ChatService.Domain.Message;
using ChatService.Domain.ValueObjects;
using Wolverine;

namespace ChatService.Application.Features.SendMessage;

public static class SendMessageHandler
{
    public static IEnumerable<object> Handle(
        SendMessageCommand command,
        IEventStoreRepository<MessageAggregate> repository)
    {
        var aggregate = MessageAggregate.Create(command.Id, command.SessionId, command.SenderId, command.Content, MessageRole.User);

        repository.Save(aggregate, expectedVersion: 0);

        return aggregate.DequeueUncommittedEvents();
    }
}
