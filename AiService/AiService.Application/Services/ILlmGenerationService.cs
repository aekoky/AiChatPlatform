using Microsoft.Extensions.AI;

namespace AiService.Application.Services;

public interface ILlmGenerationService
{
    IAsyncEnumerable<string> GenerateStreamingAsync(List<ChatMessage> messages, CancellationToken ct = default);
}
