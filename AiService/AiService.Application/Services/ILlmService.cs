using AiService.Application.Dtos;
using BuildingBlocks.Contracts.Models;

namespace AiService.Application.Services;

public interface ILlmService
{
    /// <summary>
    /// Generates a streaming response from conversation history with optional RAG context.
    /// </summary>
    IAsyncEnumerable<string> GenerateAsync(
        IReadOnlyList<ChatTurn> messages,
        RagToolResult? ragResult = null,
        CancellationToken ct = default);

    /// <summary>
    /// Summarizes a conversation into a concise title.
    /// </summary>
    Task<string> SummarizeAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken ct = default);
}