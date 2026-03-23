using AiService.Application.Services;
using BuildingBlocks.Contracts.LlmEvents;

namespace AiService.Application.Handlers;

public class CancelLlmGenerationHandler(LlmCancellationRegistry registry)
{
    public void Handle(CancelLlmGenerationEvent command)
    {
        registry.Cancel(command.RequestId);
    }
}
