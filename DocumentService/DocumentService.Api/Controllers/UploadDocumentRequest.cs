namespace DocumentService.Api.Controllers;

public record UploadDocumentRequest
{
    public IFormFile? File { get; init; }
    public string? Url { get; init; }
    public string Scope { get; init; } = "user";
    public Guid? SessionId { get; init; }
}
