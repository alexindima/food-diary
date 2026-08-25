using Amazon.S3;
using Amazon.S3.Model;
using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;

namespace FoodDiary.Integrations.Services;

internal sealed class S3ObjectStorageClient(IAmazonS3 s3Client) : IObjectStorageClient {
    public string GetPreSignedUploadUrl(
        string bucketName,
        string key,
        string contentType,
        long contentLength,
        DateTime expiresAt) {
        var request = new GetPreSignedUrlRequest {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = contentType,
        };
        request.Headers.ContentLength = contentLength;

        return s3Client.GetPreSignedURL(request);
    }

    public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) {
        var request = new DeleteObjectRequest {
            BucketName = bucketName,
            Key = key,
        };

        return s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task PutObjectBytesAsync(
        string bucketName,
        string key,
        string contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken) {
        var stream = new MemoryStream(content.ToArray(), writable: false);
        await using ConfiguredAsyncDisposable _ = stream.ConfigureAwait(false);
        var request = new PutObjectRequest {
            BucketName = bucketName,
            Key = key,
            ContentType = contentType,
            InputStream = stream,
        };

        await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StoredObjectInfo?> GetObjectInfoAsync(
        string bucketName,
        string key,
        CancellationToken cancellationToken) {
        try {
            GetObjectMetadataResponse response = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest {
                BucketName = bucketName,
                Key = key,
            }, cancellationToken).ConfigureAwait(false);

            return new StoredObjectInfo(response.ContentLength, response.Headers.ContentType);
        } catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }
    }

    public async Task<byte[]?> GetObjectBytesAsync(
        string bucketName,
        string key,
        long maximumBytes,
        CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);

        try {
            using GetObjectResponse response = await s3Client.GetObjectAsync(new GetObjectRequest {
                BucketName = bucketName,
                Key = key,
            }, cancellationToken).ConfigureAwait(false);
            Stream responseStream = response.ResponseStream;
            var content = new ArrayBufferWriter<byte>();
            while (true) {
                Memory<byte> buffer = content.GetMemory(81920);
                int bytesRead = await responseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) {
                    return content.WrittenSpan.ToArray();
                }

                if (content.WrittenCount + bytesRead > maximumBytes) {
                    throw new InvalidDataException("Stored object exceeds the allowed download size.");
                }

                content.Advance(bytesRead);
            }
        } catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }
    }
}
