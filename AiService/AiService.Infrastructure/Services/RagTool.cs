using AiService.Application.Dtos;
using AiService.Application.Services;
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
    private const double RelevanceThreshold = 0.5;

    public async Task<bool> ShouldInvokeAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default)
    {
        var recentHistory = messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .TakeLast(5)
            .ToList();

        if (recentHistory.Count == 0 || recentHistory.Last().Role != "user")
        {
            return false; // Nothing to do if no user message
        }

        var latestInput = recentHistory.Last().Content;

        var prompt = string.Format(options.Value.RagDecisionPrompt, latestInput);
        
        try
        {
            var response = await chatClient.GetResponseAsync(prompt, cancellationToken: ct);
            var decision = response.Text?.Trim().ToLowerInvariant() ?? "";
            
            return decision.Contains("yes") || decision.Contains("true");
        }
        catch
        {
            // Default to yes if the fast check fails, so we don't accidentally block valid RAG requests.
            return true;
        }
    }

    public async Task<RagToolResult> ExecuteAsync(
        string userQuery, 
        Guid userId, 
        Guid? sessionId, 
        CancellationToken ct = default)
    {
        var retrievedChunks = await retrievalService.RetrieveAsync(userQuery, userId, sessionId, topK: 15, ct);

        // Relevance Gate
        var relevantChunks = retrievedChunks
            .Where(c => c.RelevanceScore >= RelevanceThreshold)
            .DistinctBy(c => c.Content)
            .Take(5)
            .ToList();

        if (relevantChunks.Count == 0)
        {
            return new RagToolResult(false, string.Empty, []);
        }

        var uniqueChunks = relevantChunks.Select(c => new
        {
            Chunk = c,
            Source = $"{c.FileName ?? "Unknown"} | {(c.PageNumber.HasValue ? $"page {c.PageNumber}" : $"chunk {c.ChunkIndex}")}"
        }).ToList();

        var ragContext = string.Join("\n\n", uniqueChunks.Select(x => 
            $"Source: {x.Source}\n{x.Chunk.Content}"));

        var sourceReferences = uniqueChunks
            .Select(x => x.Source)
            .Distinct()
            .ToList();

        return new RagToolResult(true, ragContext, sourceReferences);
    }
}
