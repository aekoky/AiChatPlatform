namespace NotificationService.Application.Services;

public interface IStreamBufferService
{
    void Expect(Guid requestId, int tokenCount);

    bool TokensDelivered(Guid requestId, int count);

    bool IsComplete(Guid requestId);

    void Clear(Guid requestId);
}
