namespace NotificationService.Application.Services;

public interface IStreamBufferService
{
    void Expect(Guid requestId, int tokenCount);

    bool TokenDelivered(Guid requestId);

    bool IsComplete(Guid requestId);

    void Clear(Guid requestId);
}
