using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.ValueObjects;
using Microsoft.Extensions.AI;
using System.Text;
using Wolverine;

namespace AiService.Application.Commands;

public class GenerateAiResponseHandler(IChatClient chatClient)
{
    private const int MaxRetries = 3;

    public async Task Handle(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        try
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await GenerateAndPublish(message, context, ct);
                    return;
                }
                catch (Exception) when (attempt < MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                }
            }

            // All retries exhausted
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                GaveUpReasons.MaxRetriesExceeded));
        }
        catch (OperationCanceledException)
        {
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                GaveUpReasons.Timeout));
            throw;
        }
        catch (Exception)
        {
            await context.PublishAsync(new LlmResponseGaveUpEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                GaveUpReasons.LlmError));
        }
    }

    private async Task GenerateAndPublish(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, message.Prompt)
        };

        var fullResponse = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            var token = update.Text;
            if (string.IsNullOrEmpty(token)) continue;

            fullResponse.Append(token);

            await context.PublishAsync(
                new LlmTokenGeneratedEvent(message.RequestId, message.UserId, message.SessionId, token));
        }

        await context.PublishAsync(
            new LlmResponseCompletedEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                fullResponse.ToString()));
    }
}