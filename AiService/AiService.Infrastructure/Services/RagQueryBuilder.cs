using AiService.Application.Services;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Text;

namespace AiService.Infrastructure.Services;

public class RagQueryBuilder(IChatClient chatClient, IOptionsSnapshot<AiPromptOptions> options) : IRagQueryBuilder
{
    public async Task<string> BuildQueryAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default)
    {
        string userQuery;
        var recentHistory = messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .Take(5).Reverse().ToList();

        if (recentHistory.Count <= 1)
        {
            return recentHistory.FirstOrDefault()?.Content ?? string.Empty;
        }

        var historyText = string.Join("\n", recentHistory.SkipLast(1).Select(m => $"{m.Role}: {m.Content}"));
        var latestInput = recentHistory.LastOrDefault()?.Content;

        var rewritePrompt = string.Format(options.Value.RewritePrompt, historyText, latestInput);

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
            {
                userQuery = latestInput ?? string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
            userQuery = latestInput ?? string.Empty;
        }
        catch (Exception)
        {
            userQuery = latestInput ?? string.Empty;
        }

        return userQuery;
    }
}
