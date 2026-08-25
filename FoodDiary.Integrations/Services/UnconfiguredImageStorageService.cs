using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Integrations.Services;

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

    public Task DeleteAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(objectKey)
            ? Task.CompletedTask
            : Task.FromException(new InvalidOperationException(ErrorMessage));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        DeleteAsync(objectKey, isConfirmed: true, cancellationToken);

    public Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ImageObjectValidationResult result = string.IsNullOrWhiteSpace(objectKey)
            ? new ImageObjectValidationResult(IsValid: false, "invalid_key", "Image object key is required.")
            : new ImageObjectValidationResult(IsValid: false, "storage_not_configured", ErrorMessage);
        return Task.FromResult(result);
    }

    public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) =>
        ConfirmUploadedObjectAsync(objectKey, cancellationToken);
}
