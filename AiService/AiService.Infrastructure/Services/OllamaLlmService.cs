using AiService.Application.Dtos;
using AiService.Application.Services;
using AiService.Infrastructure.Extensions;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace AiService.Infrastructure.Services;

public class OllamaLlmService(
    IChatClient chatClient,
    IOptionsSnapshot<AiPromptOptions> promptOptions) : ILlmService
{
    private AiPromptOptions Prompts => promptOptions.Value;

    public async IAsyncEnumerable<string> GenerateAsync(
        IReadOnlyList<ChatTurn> messages,
        RagToolResult? ragResult = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chatMessages = messages.ToChatMessages().ToList();

        if (ragResult is { Success: true } && !string.IsNullOrWhiteSpace(ragResult.ContextString))
        {
            var ragPrompt = string.Format(Prompts.RagSystemPrompt, ragResult.ContextString);

            var lastUserIndex = chatMessages.FindLastIndex(m => m.Role == ChatRole.User);
            if (lastUserIndex >= 0)
                chatMessages.Insert(lastUserIndex, new(ChatRole.System, ragPrompt));
            else
                chatMessages.Insert(0, new(ChatRole.System, ragPrompt));
        }

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, ChatOptionsFactory.CreateAnswerOptions(), cancellationToken: ct))
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
    }

    public async Task<string> SummarizeAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken ct = default)
    {
        var chatMessages = new List<ChatMessage> { new(ChatRole.System, Prompts.SummarizeConversationPrompt) };
        chatMessages.AddRange(messages.ToChatMessages());

        var response = await chatClient.GetResponseAsync(chatMessages, ChatOptionsFactory.CreateSummaryOptions(), cancellationToken: ct);
        return response.Text.Trim();
    }
}