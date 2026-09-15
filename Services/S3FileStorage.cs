using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MyApi.Services.Interfaces.Images;
using MyApi.Shared.Configuration;

namespace MyApi.Services;

public class S3FileStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IFileStorage
{
    private const string CacheControl = "public, max-age=31536000, immutable";

    public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(content, writable: false);

        var request = new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false
        };
        request.Headers.CacheControl = CacheControl;

        await s3.PutObjectAsync(request, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default) =>
        s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = options.Value.BucketName, Key = key }, ct);
}
