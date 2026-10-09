using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Infrastructure.Providers.Services;

internal sealed class UnconfiguredImageStorageService : IImageStorageService {
    private const string ErrorMessage = "Image storage is not configured.";

    public Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<PresignedUpload>(new InvalidOperationException(ErrorMessage));
    }

    public Task DeleteAsync(ObjectStorageKey key, bool isConfirmed, CancellationToken cancellationToken) {
        string objectKey = key.Value;
        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(objectKey)
            ? Task.CompletedTask
            : Task.FromException(new InvalidOperationException(ErrorMessage));
    }

    public Task DeleteAsync(ObjectStorageKey key, CancellationToken cancellationToken) =>
        DeleteAsync(key, isConfirmed: true, cancellationToken);

    public Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(
        ObjectStorageKey key,
        CancellationToken cancellationToken) {
        string objectKey = key.Value;
        cancellationToken.ThrowIfCancellationRequested();
        ImageObjectValidationResult result = string.IsNullOrWhiteSpace(objectKey)
            ? new ImageObjectValidationResult(IsValid: false, "invalid_key", "Image object key is required.")
            : new ImageObjectValidationResult(IsValid: false, "storage_not_configured", ErrorMessage);
        return Task.FromResult(result);
    }

    public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        ObjectStorageKey key,
        CancellationToken cancellationToken) =>
        ConfirmUploadedObjectAsync(key, cancellationToken);
}
