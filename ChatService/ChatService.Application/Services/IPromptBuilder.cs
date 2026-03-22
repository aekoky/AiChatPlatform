using BuildingBlocks.Contracts.Models;

namespace ChatService.Application.Services;

public interface IPromptBuilder
{
    Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct = default);
}
