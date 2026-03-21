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
            _ => (tokenCount, 0),
            (_, current) => (tokenCount, current.Delivered));
    }

    public bool TokensDelivered(Guid requestId, int count)
    {
        var isComplete = false;
        _buffer.AddOrUpdate(
            requestId,
            _ => (Expected: -1, Delivered: count),
            (_, current) =>
            {
                var newDelivered = current.Delivered + count;
                isComplete = current.Expected >= 0 && newDelivered >= current.Expected;
                return (current.Expected, newDelivered);
            });
        return isComplete;
    }

    public bool IsComplete(Guid requestId)
    {
        if (!_buffer.TryGetValue(requestId, out var counts))
            return false;
        return counts.Expected >= 0 && counts.Delivered >= counts.Expected;
    }

    public void Clear(Guid requestId)
        => _buffer.TryRemove(requestId, out _);
}