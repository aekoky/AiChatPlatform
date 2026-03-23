using Amazon.S3;
using Amazon.S3.Model;
using DocumentIngestion.Application.Services;
using DocumentIngestion.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DocumentIngestion.Infrastructure.Services;

public class S3StorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IStorageService
{
    private readonly string _bucket = options.Value.Bucket;

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key
            };

            var response = await s3Client.GetObjectAsync(request, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex)
        {
            throw new DocumentIngestion.Application.Exceptions.StorageException($"Failed to download {key} from S3.", ex);
        }
    }
}