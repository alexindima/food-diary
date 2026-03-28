using Amazon.S3;
using Amazon.S3.Model;

namespace FoodDiary.Infrastructure.Services;

internal sealed class S3ObjectStorageClient(IAmazonS3 s3Client) : IObjectStorageClient {
    public string GetPreSignedUrl(GetPreSignedUrlRequest request) => s3Client.GetPreSignedURL(request);

    public Task DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken) =>
        s3Client.DeleteObjectAsync(request, cancellationToken);
}
