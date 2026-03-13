using BuildingBlocks.Core;
using ChatService.Domain.Message;

namespace ChatService.Application.Features.SendMessage;

public static class SendMessageHandler
{
    public static async Task Handle(
        SendMessageCommand command,
        IEventStoreRepository<MessageAggregate> repository,
        CancellationToken ct)
    {
        var message = MessageAggregate.Create(command.Id, command.SessionId, command.SenderId, command.Content);

        await repository.SaveAsync(message, expectedVersion: 0, ct).ConfigureAwait(false);
    }
}
