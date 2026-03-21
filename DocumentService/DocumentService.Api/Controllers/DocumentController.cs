using DocumentService.Api.Extensions;
using DocumentService.Application.Dtos;
using DocumentService.Application.Features.DeleteDocument;
using DocumentService.Application.Features.UploadDocument;
using DocumentService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace DocumentService.Api.Controllers;

public record UploadDocumentRequest
{
    public IFormFile File { get; init; } = null!;
    public string Scope { get; init; } = "user";
    public Guid? SessionId { get; init; }
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DocumentController(
    IMessageBus messageBus,
    IDocumentRepository repository) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("File is required.");

        var command = new UploadDocumentCommand(
            Id: Guid.NewGuid(),
            UserId: User.GetUserId(),
            SessionId: request.SessionId,
            Scope: request.Scope,
            FileName: request.File.FileName,
            ContentType: request.File.ContentType,
            FileStream: request.File.OpenReadStream());

        await messageBus.InvokeAsync(command, ct);

        return Accepted(new { command.Id });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? scope,
        [FromQuery] Guid? sessionId,
        CancellationToken ct)
        => Ok(await repository.ListAsync(User.GetUserId(), scope, sessionId, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await repository.GetByIdAsync(id, User.GetUserId(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(DocumentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var result = await repository.GetStatusAsync(id, User.GetUserId(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteDocumentCommand(id, User.GetUserId());
        await messageBus.InvokeAsync(command, ct);
        return NoContent();
    }
}