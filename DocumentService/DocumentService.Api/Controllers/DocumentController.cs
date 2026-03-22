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
    public IFormFile? File { get; init; }
    public string? Url { get; init; }
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
        [FromServices] IHttpClientFactory clientFactory,
        CancellationToken ct)
    {
        if ((request.File is null || request.File.Length == 0) && string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("File or Url is required.");

        Guid documentId = Guid.NewGuid();
        Stream fileStream;
        string fileName;
        string contentType;

        if (request.File is not null && request.File.Length > 0)
        {
            fileStream = request.File.OpenReadStream();
            fileName = request.File.FileName;
            contentType = request.File.ContentType;
        }
        else
        {
            var client = clientFactory.CreateClient();
            var response = await client.GetAsync(request.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Failed to download file from URL.");
            
            fileStream = await response.Content.ReadAsStreamAsync(ct);
            fileName = Path.GetFileName(new Uri(request.Url!).AbsolutePath);
            contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        }

        var command = new UploadDocumentCommand(
            Id: documentId,
            UserId: User.GetUserId(),
            SessionId: request.SessionId,
            Scope: request.Scope,
            FileName: fileName,
            ContentType: contentType,
            FileStream: fileStream);

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