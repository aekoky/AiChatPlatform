using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Core;
using ChatService.Application.Services;
using ChatService.Domain.Message;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;
using ChatService.Domain.ValueObjects;
using Wolverine;
using Wolverine.Persistence.Sagas;

namespace ChatService.Application.Sagas;

public class ConversationSaga : Saga
{
    public Guid Id { get; set; }
    public bool IsProcessing { get; set; }
    public Guid? ActiveRequestId { get; set; }
    public Queue<Guid> PendingMessageIds { get; set; } = new();

    public static ConversationSaga Start(SessionCreatedEvent e)
        => new() { Id = e.Id };

    public async Task Handle(
        [SagaIdentityFrom(nameof(MessageCreatedEvent.SessionId))] MessageCreatedEvent message,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        if (message.Role != MessageRole.User)
            return;

        if (!IsProcessing)
            await StartProcessing(message.SessionId, promptBuilder, context, cancellationToken);
        else
            PendingMessageIds.Enqueue(message.Id);
    }

    public async Task Handle(
        [SagaIdentityFrom(nameof(LlmResponseCompletedEvent.SessionId))] LlmResponseCompletedEvent message,
        IEventStoreRepository<MessageAggregate> repository,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        if (ActiveRequestId != message.RequestId)
            return;

        var aiMessage = MessageAggregate.Create(
            Guid.NewGuid(),
            message.SessionId,
            message.RequestId,
            message.FullResponse,
            MessageRole.Assistant);

        repository.Save(aiMessage, 0);
        ActiveRequestId = null;

        if (PendingMessageIds.TryDequeue(out _))
            await StartProcessing(message.SessionId, promptBuilder, context, cancellationToken);
        else
            IsProcessing = false;
    }

    // AiService gave up — unblock the saga so new user messages can still be processed
    public async Task Handle(
        [SagaIdentityFrom(nameof(LlmResponseGaveUpEvent.SessionId))] LlmResponseGaveUpEvent message)
    {
        if (ActiveRequestId != message.RequestId)
            return;

        ActiveRequestId = null;
        IsProcessing = false;

        // Drain pending queue — they won't get responses either, discard or re-enqueue
        PendingMessageIds.Clear();
    }

    public void Handle(SessionDeletedEvent _) => MarkCompleted();

    private async Task StartProcessing(
        Guid sessionId,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        IsProcessing = true;
        ActiveRequestId = Guid.NewGuid();

        var prompt = await promptBuilder.BuildAsync(sessionId, cancellationToken);

        await context.PublishAsync(new LlmResponseRequestedEvent(
            ActiveRequestId.Value,
            sessionId,
            prompt));
    }
}