namespace DocumentIngestion.Application.Services;

public interface IStorageService
{
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
}
