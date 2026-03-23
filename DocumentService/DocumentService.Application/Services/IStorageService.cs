namespace DocumentService.Application.Services;

public interface IStorageService
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}