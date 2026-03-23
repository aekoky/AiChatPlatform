using DocumentService.Api.Extensions;
using DocumentService.Application.Dtos;
using DocumentService.Application.Features.DeleteDocument;
using DocumentService.Application.Features.UploadDocument;
using DocumentService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace DocumentService.Api.Controllers;

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
        if ((request.File is null || request.File.Length == 0) && string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("File or Url is required.");
        var documentId = Guid.NewGuid();
        var command = new UploadDocumentCommand(
            Id: documentId,
            UserId: User.GetUserId(),
            SessionId: request.SessionId,
            Scope: request.Scope,
            File: request.File?.OpenReadStream(),
            FileName: request.File?.FileName,
            ContentType: request.File?.ContentType,
            Url: request.Url);

        await messageBus.InvokeAsync(command, ct);

        return Accepted(new { Id = documentId });
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