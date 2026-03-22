using AiService.Application.Dtos;
using AiService.Application.Services;
using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.ValueObjects;
using Microsoft.Extensions.AI;
using System.Text;
using Wolverine;

namespace AiService.Application.Commands;

public class GenerateAiResponseHandler(
    IRagQueryBuilder queryBuilder,
    IRagTool ragTool,
    IPromptComposer promptComposer,
    ILlmGenerationService llmGenerationService,
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
        var shouldRag = await ragTool.ShouldInvokeAsync(message.Messages, ct);
        RagToolResult? ragResult = null;

        if (shouldRag)
        {
            var userQuery = await queryBuilder.BuildQueryAsync(message.Messages, ct);

            try
            {
                using var ragCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ragCts.CancelAfter(TimeSpan.FromSeconds(10));

                ragResult = await ragTool.ExecuteAsync(
                    userQuery,
                    message.UserId,
                    message.SessionId,
                    ragCts.Token);
            }
            catch (OperationCanceledException)
            {
                // RAG timed out
            }
        }

        var (chatMessages, sourceReferences) = promptComposer.Compose(message.Messages, ragResult);

        var fullResponse = new StringBuilder();
        var totalTokenCount = 0;
        var currentBatch = new StringBuilder();
        var batchTokenCount = 0;
        var lastPublishTime = DateTime.UtcNow;
        var publishInterval = TimeSpan.FromMilliseconds(200);

        await foreach (var token in llmGenerationService.GenerateStreamingAsync(chatMessages, ct))
        {

            fullResponse.Append(token);
            currentBatch.Append(token);
            totalTokenCount++;
            batchTokenCount++;

            if (DateTime.UtcNow - lastPublishTime > publishInterval)
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
                lastPublishTime = DateTime.UtcNow;
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
                totalTokenCount,
                sourceReferences));
    }

}
