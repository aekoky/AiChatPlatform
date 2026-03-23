using Microsoft.Extensions.AI;

namespace AiService.Infrastructure.Options;

public static class ChatOptionsFactory
{
    public static ChatOptions CreateDecisionOptions() => new()
    {
        Temperature = 0,
        TopP = 1,
        MaxOutputTokens = 4
    };

    public static ChatOptions CreateRewriteOptions() => new()
    {
        Temperature = 0,
        TopP = 1,
        MaxOutputTokens = 128
    };

    public static ChatOptions CreateSummaryOptions() => new()
    {
        Temperature = 0.2f,
        TopP = 1,
        MaxOutputTokens = 48
    };

    public static ChatOptions CreateAnswerOptions() => new()
    {
        Temperature = 0.7f,
        TopP = 1,
        MaxOutputTokens = 800
    };
}