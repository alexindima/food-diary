using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageStorageService {
    Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken) =>
        DeleteAsync(objectKey, cancellationToken);

    Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(string objectKey, CancellationToken cancellationToken);

    Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(string objectKey, CancellationToken cancellationToken) =>
        ValidateUploadedObjectAsync(objectKey, cancellationToken);
}
