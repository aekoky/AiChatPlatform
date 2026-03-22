using AiService.Application.Services;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AiService.Infrastructure.Services;

public class LlmGenerationService(IChatClient chatClient) : ILlmGenerationService
{
    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        List<ChatMessage> messages, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            var token = update.Text;
            if (!string.IsNullOrEmpty(token))
            {
                yield return token;
            }
        }
    }
}
