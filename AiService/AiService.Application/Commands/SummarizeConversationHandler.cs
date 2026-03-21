using BuildingBlocks.Contracts.SessionEvents;
using Microsoft.Extensions.AI;
using System.Text;
using Wolverine;

namespace AiService.Application.Commands;

public class SummarizeConversationHandler(IChatClient chatClient)
{
    public async Task Handle(
        SessionSummarizeRequestedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        foreach (var m in message.Messages)
        {
            sb.AppendLine($"{m.Role}: {m.Content}");
        }

        var prompt = $"""
            Summarize the following conversation in one short paragraph (max 3 sentences). 
            Focus on the key topics discussed and any decisions made.
            
            Conversation:
            {sb}
            """;

        var sbResponse = new StringBuilder();
        var chatMessages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                sbResponse.Append(update.Text);
            }
        }

        var summary = sbResponse.ToString().Trim();

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
