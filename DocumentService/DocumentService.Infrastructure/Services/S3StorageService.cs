using Amazon.S3;
using Amazon.S3.Model;
using DocumentService.Application.Services;
using DocumentService.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DocumentService.Infrastructure.Services;

public class S3StorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IStorageService
{
    private readonly string _bucket = options.Value.Bucket;

    public async Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            };

            await s3Client.PutObjectAsync(request, ct);
        }
        catch (AmazonS3Exception ex)
        {
            throw new DocumentService.Application.Exceptions.StorageException($"Failed to upload {key} to S3.", ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = key
            };

            await s3Client.DeleteObjectAsync(request, ct);
        }
        catch (AmazonS3Exception ex)
        {
            throw new DocumentService.Application.Exceptions.StorageException($"Failed to delete {key} from S3.", ex);
        }
    }
}