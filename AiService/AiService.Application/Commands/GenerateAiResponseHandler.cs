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
                    await context.PublishAsync(new LlmResponseRetryingEvent(
                       message.RequestId,
                       message.SessionId,
                       message.UserId));

                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                }
                catch (Exception) when (attempt == MaxRetries)
                {
                    await context.PublishAsync(new LlmResponseGaveUpEvent(
                        message.RequestId,
                        message.SessionId,
                        message.UserId,
                        GaveUpReasons.MaxRetriesExceeded));
                    return;
                }
            }
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
        var messages = message.Messages
            .Select(t => new ChatMessage(
                t.Role == "system"    ? ChatRole.System    :
                t.Role == "assistant" ? ChatRole.Assistant : ChatRole.User,
                t.Content))
            .ToList();

        var fullResponse = new StringBuilder();
        var tokenCount = 0;
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            var token = update.Text;
            if (string.IsNullOrEmpty(token)) continue;

            fullResponse.Append(token);
            tokenCount++;

            await context.PublishAsync(
                new LlmTokenGeneratedEvent(message.RequestId, message.SessionId, message.UserId, token));
        }

        await context.PublishAsync(
            new LlmResponseCompletedEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                fullResponse.ToString(),
                tokenCount));
    }
}