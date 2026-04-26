namespace FoodDiary.Integrations.Services;

public interface IObjectStorageClient {
    string GetPreSignedUploadUrl(
        string bucketName,
        string key,
        string contentType,
        DateTime expiresAt);

    Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken);
}
