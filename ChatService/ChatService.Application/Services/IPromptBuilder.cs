using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChatService.Application.Services;

public interface IPromptBuilder
{
    Task<string> BuildAsync(Guid sessionId, CancellationToken ct);
}
