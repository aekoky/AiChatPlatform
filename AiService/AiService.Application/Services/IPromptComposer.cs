using AiService.Application.Dtos;
using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;

namespace AiService.Application.Services;

public interface IPromptComposer
{
    (List<ChatMessage> Messages, IReadOnlyList<string> SourceReferences) Compose(
        IReadOnlyList<ChatTurn> originalMessages, 
        RagToolResult? ragResult);
}
