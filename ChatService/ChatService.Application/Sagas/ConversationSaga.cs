using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.Models;
using BuildingBlocks.Contracts.SessionEvents;
using BuildingBlocks.Contracts.ValueObjects;
using BuildingBlocks.Core;
using ChatService.Application.Dtos;
using ChatService.Application.Services;
using ChatService.Domain.Message;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session;
using ChatService.Domain.Session.Events;
using ChatService.Domain.ValueObjects;
using JasperFx.Core;
using Wolverine;
using Wolverine.Persistence.Sagas;

namespace ChatService.Application.Sagas;

// Scheduled via TimeoutMessage — fires if LLM never responds
public record ConversationProcessingTimeout(Guid SessionId, Guid RequestId)
    : TimeoutMessage(2.Minutes());

public class ConversationSaga : Saga
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsProcessing { get; set; }
    public Guid? ActiveRequestId { get; set; }
    public Queue<Guid> PendingMessageIds { get; set; } = new();
    public int TurnCount { get; set; }
    public bool HasGeneratedTitle { get; set; }

    public static ConversationSaga Start(SessionCreatedEvent e)
        => new() { Id = e.Id, UserId = e.UserId };

    public async Task Handle(
        [SagaIdentityFrom(nameof(MessageCreatedEvent.SessionId))] MessageCreatedEvent message,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        if (message.Role != MessageRole.User)
            return;

        if (!IsProcessing)
            await StartProcessing(message.Id, message.SessionId, message.SenderId, promptBuilder, context, cancellationToken);
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
            message.UserId,
            message.FullResponse,
            MessageRole.Assistant,
            message.Sources);

        repository.Save(aiMessage);
        ActiveRequestId = null;
        TurnCount++;

        // Auto-generate title after first assistant response if missing
        if (!HasGeneratedTitle)
        {
            await TriggerSummarization(promptBuilder, context, cancellationToken);
            HasGeneratedTitle = true;
        }
        // Rolling summary every 10 turns
        else if (TurnCount % 10 == 0)
        {
            await TriggerSummarization(promptBuilder, context, cancellationToken);
        }

        if (PendingMessageIds.TryDequeue(out Guid requestId))
            await StartProcessing(requestId, Id, UserId, promptBuilder, context, cancellationToken);
        else
            IsProcessing = false;
    }

    public async Task Handle(
        SessionSummaryGeneratedEvent message,
        IEventStoreRepository<SessionAggregate> repository,
        IReadOnlyEventStore readOnlyEventStore,
        IMessageContext context)
    {
        var conversation = await readOnlyEventStore.QueryFirstOrDefaultAsync<ConversationDto>(
            q => q.Where(c => c.Id == message.SessionId)).ConfigureAwait(false);
        if (conversation == null) return;
        var session = await repository.LoadAsync(message.SessionId, conversation.Version);
        if (session == null) return;

        session.UpdateSummary(message.Summary);

        // Auto-generate title if it's still the default
        if (string.IsNullOrWhiteSpace(session.Title) || session.Title == "New Chat")
        {
            // Truncate summary for title
            var title = message.Summary.Length > 50
                ? message.Summary[..47] + "..."
                : message.Summary;

            session.UpdateTitle(title);
            await context.PublishAsync(new SessionTitleUpdatedNotificationEvent(Id, UserId, title));
        }

        await context.PublishAsync(new SessionSummaryUpdatedNotificationEvent(Id, UserId, message.Summary));
        repository.Save(session);
    }

    private async Task TriggerSummarization(IPromptBuilder promptBuilder, IMessageContext context, CancellationToken ct)
    {
        var messages = await promptBuilder.BuildAsync(Id, ct);

        // Map messages to Contracts DTOs for the AI service
        var dtoMessages = messages.Select(m => new ChatTurn(m.Role, m.Content)).ToList();

        await context.PublishAsync(new SessionSummarizeRequestedEvent(
            Guid.NewGuid(),
            Id,
            UserId,
            dtoMessages));
    }

    public async Task Handle(
        [SagaIdentityFrom(nameof(LlmResponseGaveUpEvent.SessionId))] LlmResponseGaveUpEvent message,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        if (ActiveRequestId != message.RequestId)
            return;

        ActiveRequestId = null;

        // Attempt next pending message instead of discarding the queue
        if (PendingMessageIds.TryDequeue(out Guid nextMessageId))
            await StartProcessing(nextMessageId, Id, UserId, promptBuilder, context, cancellationToken);
        else
            IsProcessing = false;
    }

    // Fires if LLM never responds — unblocks the saga
    public async Task Handle(
        [SagaIdentityFrom(nameof(ConversationProcessingTimeout.SessionId))] ConversationProcessingTimeout timeout,
        IMessageContext context,
        IPromptBuilder promptBuilder,
        CancellationToken cancellationToken)
    {
        // Only act if this timeout matches the current active request
        if (ActiveRequestId != timeout.RequestId)
            return;

        await context.PublishAsync(new LlmResponseGaveUpEvent(
            timeout.RequestId,
            timeout.SessionId,
            UserId,
            GaveUpReasons.Timeout));

        // Explicitly command the AiWorker to nuke its running thread
        await context.PublishAsync(new CancelLlmGenerationEvent(timeout.RequestId));

        ActiveRequestId = null;

        // Attempt next pending message
        if (PendingMessageIds.TryDequeue(out Guid nextMessageId))
            await StartProcessing(nextMessageId, Id, UserId, promptBuilder, context, cancellationToken);
        else
            IsProcessing = false;
    }

    // Required by Wolverine — handles timeout arriving after saga completes
    public static void NotFound(ConversationProcessingTimeout timeout) { }

    public async Task Handle(
        [SagaIdentityFrom(nameof(SessionDeletedEvent.Id))] SessionDeletedEvent message,
        IMessageContext context)
    {
        if (IsProcessing && ActiveRequestId.HasValue)
        {
            await context.PublishAsync(new CancelLlmGenerationEvent(ActiveRequestId.Value));
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                ActiveRequestId.Value,
                message.Id,
                UserId,
                GaveUpReasons.SessionDeleted));
        }

        MarkCompleted();
    }

    private async Task StartProcessing(
        Guid requestId,
        Guid sessionId,
        Guid userId,
        IPromptBuilder promptBuilder,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        IsProcessing = true;
        ActiveRequestId = requestId;

        var messages = await promptBuilder.BuildAsync(sessionId, cancellationToken);

        await context.PublishAsync(new LlmResponseRequestedEvent(
            ActiveRequestId.Value,
            sessionId,
            userId,
            messages));

        // Schedule a durable timeout — fires if LLM never responds
        await context.ScheduleAsync(
            new ConversationProcessingTimeout(sessionId, requestId),
            20.Minutes());
    }
}