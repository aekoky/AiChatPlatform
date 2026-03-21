using BuildingBlocks.Contracts.LlmEvents;
using NotificationService.Application.Services;

namespace NotificationService.Application.Commands;

public class LlmResponseRetryingHandler(IStreamBufferService streamBuffer)
{
    public void Handle(
        LlmResponseRetryingEvent message)
    {
        streamBuffer.Clear(message.RequestId);
    }
}