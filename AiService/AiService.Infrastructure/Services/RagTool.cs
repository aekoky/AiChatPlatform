using AiService.Application.Dtos;
using AiService.Application.Services;
using AiService.Infrastructure.Extensions;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Services;

public class RagTool(
    IChatClient chatClient,
    IRagRetrievalService retrievalService,
    IOptionsSnapshot<AiPromptOptions> options) : IRagTool
{
    private const double RelevanceThreshold = 0.3;

    public async Task<bool> ShouldInvokeAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken ct = default)
    {
        var latestUserMessage = messages.LastOrDefault(m => m.Role == "user");

        if (latestUserMessage is null)
            return false;

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, options.Value.RagDecisionPrompt),
            new(ChatRole.User, latestUserMessage.Content)
        };

        var response = await chatClient.GetResponseAsync(chatMessages, ChatOptionsFactory.CreateDecisionOptions(), cancellationToken: ct);

        return response.Text.Trim().StartsWith("YES", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> BuildQueryAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken ct = default)
    {
        var latestQuery = messages.LastOrDefault(m => m.Role == "user")?.Content
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(latestQuery))
            return latestQuery;

        var history = messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .SkipLast(1)
            .TakeLast(4)
            .ToList();

        if (history.Count == 0)
            return latestQuery;

        var chatMessages = new List<ChatMessage> { new(ChatRole.System, options.Value.RewritePrompt) };
        chatMessages.AddRange(history.ToChatMessages());
        chatMessages.Add(new(ChatRole.User, latestQuery));

        var response = await chatClient.GetResponseAsync(chatMessages, ChatOptionsFactory.CreateRewriteOptions(), cancellationToken: ct);
        var rewritten = response.Text?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(rewritten) ? latestQuery : rewritten;
    }

    public async Task<RagToolResult> ExecuteAsync(
        string userQuery,
        Guid userId,
        Guid? sessionId,
        CancellationToken ct = default)
    {
        var chunks = await retrievalService.RetrieveAsync(userQuery, userId, sessionId, topK: 15, ct);

        var relevant = chunks
            .Where(c => c.RelevanceScore >= RelevanceThreshold)
            .DistinctBy(c => c.Content)
            .Take(5)
            .ToList();

        if (relevant.Count == 0)
            return RagToolResult.Empty;

        var contextString = string.Join("\n\n", relevant.Select(c =>
            string.IsNullOrWhiteSpace(c.FileName)
                ? c.Content
                : $"Source: {c.FileName}\n{c.Content}"));

        var sourceReferences = relevant
            .Select(c => c.FileName)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        return new RagToolResult(true, contextString, sourceReferences!);
    }
}