using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Services;

public sealed record PresignedUpload(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpirationUtc);

public interface IImageStorageService
{
    Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
