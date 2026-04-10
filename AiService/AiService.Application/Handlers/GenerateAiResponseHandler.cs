using AiService.Application.Dtos;
using AiService.Application.Services;
using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Wolverine;

namespace AiService.Application.Handlers;

public class GenerateAiResponseHandler(
    ILlmService llmService,
    IRagTool ragTool,
    LlmCancellationRegistry cancellationRegistry,
    ILogger<GenerateAiResponseHandler> logger)
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
            // Build RAG context once — shared across all retry attempts
            var ragResult = await BuildRagContextAsync(message, linkedCts.Token);

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await GenerateAndPublish(message, context, ragResult, linkedCts.Token);
                    return;
                }
                catch (Exception) when (attempt < MaxRetries)
                {
                    await context.PublishAsync(new LlmResponseRetryingEvent(
                        message.RequestId,
                        message.SessionId,
                        message.UserId));
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), linkedCts.Token);
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
        finally
        {
            cancellationRegistry.Unregister(message.RequestId);
        }
    }

    private async Task GenerateAndPublish(
        LlmResponseRequestedEvent message,
        IMessageContext context,
        RagToolResult? ragResult,
        CancellationToken ct)
    {
        if (ragResult is { Success: true, SourceReferences.Count: > 0 })
        {
            await context.PublishAsync(new LlmSourcesFoundEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                ragResult.SourceReferences));
        }

        var fullResponse = new StringBuilder();
        var totalTokenCount = 0;
        var currentBatch = new StringBuilder();
        var batchTokenCount = 0;
        var stopwatch = Stopwatch.StartNew();
        var flushInter = TimeSpan.FromMicroseconds(150);

        await foreach (var token in llmService.GenerateAsync(message.Messages, ragResult, ct))
        {
            fullResponse.Append(token);
            currentBatch.Append(token);
            totalTokenCount++;
            batchTokenCount++;

            if (stopwatch.Elapsed >= flushInter)
            {
                await context.InvokeAsync(new LlmTokensGeneratedEvent(
                    message.RequestId,
                    message.SessionId,
                    message.UserId,
                    currentBatch.ToString(),
                    batchTokenCount));
                currentBatch.Clear();
                stopwatch.Restart();
            }
        }

        if (currentBatch.Length > 0)
        {
            await context.InvokeAsync(new LlmTokensGeneratedEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                currentBatch.ToString(),
                batchTokenCount));
        }

        await context.PublishAsync(new LlmResponseCompletedEvent(
            message.RequestId,
            message.SessionId,
            message.UserId,
            fullResponse.ToString(),
            totalTokenCount));
    }

    private async Task<RagToolResult?> BuildRagContextAsync(
        LlmResponseRequestedEvent message,
        CancellationToken ct)
    {
        try
        {
            using var decisionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            decisionCts.CancelAfter(TimeSpan.FromSeconds(500));

            if (!await ragTool.ShouldInvokeAsync(message.Messages, decisionCts.Token))
                return RagToolResult.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RAG decision failed for request {RequestId} — skipping retrieval", message.RequestId);
            return RagToolResult.Empty;
        }

        string userQuery;
        try
        {
            using var rewriteCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            rewriteCts.CancelAfter(TimeSpan.FromSeconds(500));

            userQuery = await ragTool.BuildQueryAsync(message.Messages, rewriteCts.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RAG query rewrite failed for request {RequestId} — using raw query", message.RequestId);
            userQuery = message.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(userQuery))
            return RagToolResult.Empty;

        try
        {
            using var retrieveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            retrieveCts.CancelAfter(TimeSpan.FromSeconds(500));

            return await ragTool.ExecuteAsync(
                userQuery,
                message.UserId,
                message.SessionId,
                retrieveCts.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RAG retrieval failed for request {RequestId} — proceeding without context", message.RequestId);
            return RagToolResult.Empty;
        }
    }
}