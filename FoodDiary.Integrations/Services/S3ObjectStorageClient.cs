using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

namespace FoodDiary.Integrations.Services;

internal sealed class S3ObjectStorageClient(IAmazonS3 s3Client) : IObjectStorageClient {
    public string GetPreSignedUploadUrl(
        string bucketName,
        string key,
        string contentType,
        DateTime expiresAt) {
        var request = new GetPreSignedUrlRequest {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = contentType,
        };

        return s3Client.GetPreSignedURL(request);
    }

    public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) {
        var request = new DeleteObjectRequest {
            BucketName = bucketName,
            Key = key,
        };

        return s3Client.DeleteObjectAsync(request, cancellationToken);
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
}
