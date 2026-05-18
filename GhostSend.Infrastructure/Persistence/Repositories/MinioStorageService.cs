using Amazon.S3;
using Amazon.S3.Model;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GhostSend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles file storage securely using a self-hosted MinIO instance via the Amazon S3 API.
/// This implementation processes files exclusively via Streams to ensure minimal RAM usage on low-spec hardware.
/// </summary>
public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private static bool _bucketChecked;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public MinioStorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["MinioSettings:BucketName"] ?? "ghostsend-files";
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketChecked) return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_bucketChecked) return;

            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            if (!bucketExists)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName
                };
                await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);
            }
            _bucketChecked = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string anonymousFileName, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = anonymousFileName,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = false // Manage lifecycle at the caller level
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        return anonymousFileName;
    }

    public async Task<Stream> DownloadFileAsync(string anonymousFileName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var getRequest = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = anonymousFileName
        };

        var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
        
        // Return direct response stream to act as a secure proxy
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string anonymousFileName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = anonymousFileName
        };

        await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
    }
}
