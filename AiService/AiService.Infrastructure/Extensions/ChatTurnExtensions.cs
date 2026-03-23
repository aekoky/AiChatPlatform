using BuildingBlocks.Contracts.Models;
using Microsoft.Extensions.AI;

namespace AiService.Infrastructure.Extensions;

public static class ChatTurnExtensions
{
    public static ChatMessage ToChatMessage(this ChatTurn turn) => new(
        turn.Role switch
        {
            "system" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            _ => ChatRole.User
        },
        turn.Content);

    public static IReadOnlyList<ChatMessage> ToChatMessages(this IEnumerable<ChatTurn> turns)
        => [.. turns.Select(t => t.ToChatMessage())];
}