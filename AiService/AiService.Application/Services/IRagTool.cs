using AiService.Application.Dtos;
using BuildingBlocks.Contracts.Models;

namespace AiService.Application.Services;

public interface IRagTool
{
    Task<bool> ShouldInvokeAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default);
    
    Task<RagToolResult> ExecuteAsync(
        string userQuery, 
        Guid userId, 
        Guid? sessionId, 
        CancellationToken ct = default);
}
