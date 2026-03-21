using AiService.Application.Services;
using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.ValueObjects;
using Microsoft.Extensions.AI;
using System.Text;
using Wolverine;

namespace AiService.Application.Commands;

public class GenerateAiResponseHandler(
    IChatClient chatClient,
    IRagRetrievalService ragRetrieval,
    LlmCancellationRegistry cancellationRegistry)
{
    private const int MaxRetries = 3;

    public async Task Handle(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancellationRegistry.Register(message.RequestId, linkedCts);

        try
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await GenerateAndPublish(message, context, linkedCts.Token);
                    return;
                }
                catch (Exception ex) when (attempt < MaxRetries && ex is not OperationCanceledException)
                {
                    await context.PublishAsync(new LlmResponseRetryingEvent(
                        message.RequestId,
                        message.SessionId,
                        message.UserId));
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), linkedCts.Token);
                }
                catch (Exception ex) when (attempt == MaxRetries && ex is not OperationCanceledException)
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
        finally
        {
            cancellationRegistry.Unregister(message.RequestId);
        }
    }

    private async Task GenerateAndPublish(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        var chatMessages = await BuildMessagesWithRagAsync(message, ct);

        var fullResponse = new StringBuilder();
        var totalTokenCount = 0;
        var currentBatch = new StringBuilder();
        var batchTokenCount = 0;

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
        {
            var token = update.Text;
            if (string.IsNullOrEmpty(token)) continue;

            fullResponse.Append(token);
            currentBatch.Append(token);
            totalTokenCount++;
            batchTokenCount++;

            if (batchTokenCount % 10 == 0)
            {
                await context.PublishAsync(
                    new LlmTokensGeneratedEvent(
                        message.RequestId,
                        message.SessionId,
                        message.UserId,
                        currentBatch.ToString(),
                        batchTokenCount));
                currentBatch.Clear();
                batchTokenCount = 0;
            }
        }

        // Flush remaining tokens
        if (currentBatch.Length > 0)
        {
            await context.PublishAsync(
                new LlmTokensGeneratedEvent(
                    message.RequestId,
                    message.SessionId,
                    message.UserId,
                    currentBatch.ToString(),
                    batchTokenCount));
        }

        await context.PublishAsync(
            new LlmResponseCompletedEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                fullResponse.ToString(),
                totalTokenCount));
    }

    private async Task<List<ChatMessage>> BuildMessagesWithRagAsync(
        LlmResponseRequestedEvent message,
        CancellationToken ct)
    {
        string userQuery;
        var recentHistory = message.Messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .TakeLast(5).ToList();

        if (recentHistory.Count <= 1)
        {
            userQuery = recentHistory.FirstOrDefault()?.Content ?? string.Empty;
        }
        else
        {
            var historyText = string.Join("\n", recentHistory.SkipLast(1).Select(m => $"{m.Role}: {m.Content}"));
            var latestInput = recentHistory.LastOrDefault()?.Content;

            var rewritePrompt = $"""
                Given the following conversation history, rewrite the user's latest input into a standalone, search-optimized query.
                Do not answer the question, just return the standalone query.

                History:
                {historyText}

                Latest User Input:
                {latestInput}
                """;

            try
            {
                using var rewriteCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                rewriteCts.CancelAfter(TimeSpan.FromSeconds(5));

                var rewriteMessages = new List<ChatMessage> { new(ChatRole.User, rewritePrompt) };
                var sb = new StringBuilder();
                await foreach (var update in chatClient.GetStreamingResponseAsync(
                    rewriteMessages, cancellationToken: rewriteCts.Token))
                {
                    sb.Append(update.Text);
                }
                userQuery = sb.ToString().Trim();
                if (string.IsNullOrWhiteSpace(userQuery))
                    userQuery = latestInput ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                userQuery = latestInput ?? string.Empty;
            }
            catch (Exception)
            {
                userQuery = latestInput ?? string.Empty;
            }
        }

        IReadOnlyList<string> chunks = [];
        try
        {
            using var ragCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            ragCts.CancelAfter(TimeSpan.FromSeconds(10));

            chunks = await ragRetrieval.RetrieveAsync(
                userQuery,
                message.UserId,
                message.SessionId,
                topK: 5,
                ragCts.Token);
        }
        catch (OperationCanceledException)
        {
            // RAG timed out — proceed without context
        }

        var chatMessages = message.Messages
            .Select(t => new ChatMessage(
                t.Role == "system" ? ChatRole.System :
                t.Role == "assistant" ? ChatRole.Assistant : ChatRole.User,
                t.Content))
            .ToList();

        if (chunks.Count > 0)
        {
            var ragContext = string.Join("\n\n", chunks);
            var ragSystemMessage = new ChatMessage(
                ChatRole.System,
                $"""
                Use the following context to answer the user's question.
                If the context is not relevant, ignore it and answer from your own knowledge.

                Context:
                {ragContext}
                """);

            var lastUserIndex = chatMessages.FindLastIndex(m => m.Role == ChatRole.User);
            if (lastUserIndex >= 0)
                chatMessages.Insert(lastUserIndex, ragSystemMessage);
            else
                chatMessages.Insert(0, ragSystemMessage);
        }

        return chatMessages;
    }
}