using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageStorageService {
    Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken);

    Task DeleteAsync(ObjectStorageKey key, CancellationToken cancellationToken);

    Task DeleteAsync(ObjectStorageKey key, bool isConfirmed, CancellationToken cancellationToken) =>
        DeleteAsync(key, cancellationToken);

    Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(ObjectStorageKey key, CancellationToken cancellationToken);

    Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(ObjectStorageKey key, CancellationToken cancellationToken) =>
        ValidateUploadedObjectAsync(key, cancellationToken);
}
