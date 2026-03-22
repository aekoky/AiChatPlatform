using AiService.Application.Dtos;
using AiService.Application.Services;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Services;

public class PromptComposer(IOptionsSnapshot<AiPromptOptions> options) : IPromptComposer
{
    public (List<ChatMessage> Messages, IReadOnlyList<string> SourceReferences) Compose(
        IReadOnlyList<ChatTurn> originalMessages,
        RagToolResult? ragResult)
    {
        var chatMessages = new List<ChatMessage>(originalMessages.Count + 1);
        
        foreach (var t in originalMessages)
        {
            var role = t.Role == "system" ? ChatRole.System :
                       t.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;
            chatMessages.Add(new ChatMessage(role, t.Content));
        }

        if (ragResult != null && ragResult.Success && !string.IsNullOrWhiteSpace(ragResult.ContextString))
        {
            var ragSystemMessage = new ChatMessage(
                ChatRole.System,
                string.Format(options.Value.RagSystemPrompt, ragResult.ContextString));

            var lastUserIndex = chatMessages.FindLastIndex(m => m.Role == ChatRole.User);
            if (lastUserIndex >= 0)
            {
                chatMessages.Insert(lastUserIndex, ragSystemMessage);
            }
            else
            {
                chatMessages.Insert(0, ragSystemMessage);
            }

            return (chatMessages, ragResult.SourceReferences);
        }

        return (chatMessages, []);
    }
}
