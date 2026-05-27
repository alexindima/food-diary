using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public sealed record PresignedUpload(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpirationUtc);

public sealed record ImageObjectValidationResult(bool IsValid, string? ErrorCode = null, string? Message = null);

public interface IImageStorageService {
    Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(string objectKey, CancellationToken cancellationToken);
}
