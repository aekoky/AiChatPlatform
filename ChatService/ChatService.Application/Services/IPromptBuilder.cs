using BuildingBlocks.Contracts.Events;
using System.Collections.Generic;

namespace ChatService.Application.Services;

public interface IPromptBuilder
{
    Task<IReadOnlyList<ChatTurn>> BuildAsync(Guid sessionId, CancellationToken ct);
}
