using BuildingBlocks.Contracts.Models;

namespace AiService.Application.Services;

public interface IRagQueryBuilder
{
    Task<string> BuildQueryAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default);
}
