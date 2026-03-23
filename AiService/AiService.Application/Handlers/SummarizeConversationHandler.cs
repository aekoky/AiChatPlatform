using AiService.Application.Services;
using BuildingBlocks.Contracts.SessionEvents;
using Wolverine;

namespace AiService.Application.Handlers;

public class SummarizeConversationHandler(ILlmService llmService)
{
    public async Task Handle(
        SessionSummarizeRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {

        var summary = await llmService.SummarizeAsync(message.Messages, ct);

        if (!string.IsNullOrWhiteSpace(summary))
        {
            await context.PublishAsync(new SessionSummaryGeneratedEvent(
                message.RequestId,
                message.SessionId,
                message.UserId,
                summary));
        }
    }
}
