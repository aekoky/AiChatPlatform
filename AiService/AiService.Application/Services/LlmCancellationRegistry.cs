using System.Collections.Concurrent;

namespace AiService.Application.Services;

public class LlmCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _registry = new();

    public void Register(Guid requestId, CancellationTokenSource cts)
        => _registry[requestId] = cts;

    public void Unregister(Guid requestId)
        => _registry.TryRemove(requestId, out _);

    public bool Cancel(Guid requestId)
    {
        if (!_registry.TryRemove(requestId, out var cts))
            return false;

        cts.Cancel();
        return true;
    }
}
