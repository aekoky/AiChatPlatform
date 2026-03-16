using NotificationService.Application.Services;
using System.Collections.Concurrent;

namespace NotificationService.Api.Services;

public class StreamBufferService : IStreamBufferService
{
    private readonly ConcurrentDictionary<Guid, (int Expected, int Delivered)> _buffer = new();

    public void Expect(Guid requestId, int tokenCount)
    {
        _buffer.AddOrUpdate(
            requestId,
            // Not in dictionary — set expected, delivered starts at 0
            _ => (tokenCount, 0),
            // Already in dictionary — tokens arrived early, preserve delivered count
            (_, current) => (tokenCount, current.Delivered));
    }

    public bool TokenDelivered(Guid requestId)
    {
        var isComplete = false;

        _buffer.AddOrUpdate(
            requestId,
            // Not in dictionary — Completed not arrived yet, track with sentinel -1
            _ => (Expected: -1, Delivered: 1),
            // Atomically increment and check completion
            (_, current) =>
            {
                var newDelivered = current.Delivered + 1;
                isComplete = current.Expected >= 0 && newDelivered >= current.Expected;
                return (current.Expected, newDelivered);
            });

        return isComplete;
    }

    public bool IsComplete(Guid requestId)
    {
        if (!_buffer.TryGetValue(requestId, out var counts))
            return false;

        // Only complete if Expected is set (>= 0) and all tokens delivered
        return counts.Expected >= 0 && counts.Delivered >= counts.Expected;
    }

    public void Clear(Guid requestId)
        => _buffer.TryRemove(requestId, out _);
}

