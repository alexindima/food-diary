using Amazon.S3.Model;

namespace FoodDiary.Infrastructure.Services;

public interface IObjectStorageClient {
    string GetPreSignedUrl(GetPreSignedUrlRequest request);

    Task DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken);
}
