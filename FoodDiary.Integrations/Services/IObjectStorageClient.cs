namespace FoodDiary.Integrations.Services;

public interface IObjectStorageClient {
    string GetPreSignedUploadUrl(
        string bucketName,
        string key,
        string contentType,
        long contentLength,
        DateTime expiresAt);

    Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken);

    Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken);

    Task<byte[]?> GetObjectBytesAsync(
        string bucketName,
        string key,
        long maximumBytes,
        CancellationToken cancellationToken);
}
