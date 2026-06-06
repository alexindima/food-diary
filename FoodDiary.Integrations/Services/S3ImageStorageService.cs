using System.Net.Mime;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Services;

public sealed class S3ImageStorageService(
    IObjectStorageClient storageClient,
    IOptions<S3Options> options,
    IDateTimeProvider dateTimeProvider) : IImageStorageService {
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) {
        MediaTypeNames.Image.Jpeg,
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken) {
        try {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            if (fileSizeBytes <= 0) {
                throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");
            }

            if (fileSizeBytes > _options.MaxUploadSizeBytes) {
                throw new InvalidOperationException(
                    $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes.");
            }

            if (!AllowedContentTypes.Contains(contentType)) {
                throw new InvalidOperationException($"Unsupported content type: {contentType}.");
            }

            string normalizedName = NormalizeFileName(fileName);
            string key = $"users/{userId.Value:D}/images/{Guid.NewGuid():N}-{normalizedName}";

            DateTime expiresAt = dateTimeProvider.UtcNow.AddMinutes(15);
            string uploadUrl = storageClient.GetPreSignedUploadUrl(_options.Bucket, key, contentType, expiresAt);
            string fileUrl = BuildPublicUrl(key);

            IntegrationsTelemetry.RecordStorageOperation("presign", "success");
            var result = new PresignedUpload(uploadUrl, fileUrl, key, expiresAt);
            return Task.FromResult(result);
        } catch (Exception ex) {
            IntegrationsTelemetry.RecordStorageOperation(
                "presign",
                ex is ArgumentException or InvalidOperationException ? "validation_error" : "failure",
                ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(objectKey)) {
            return;
        }

        try {
            await storageClient.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken).ConfigureAwait(false);
            IntegrationsTelemetry.RecordStorageOperation("delete", "success");
        } catch (Exception ex) {
            IntegrationsTelemetry.RecordStorageOperation("delete", "failure", ex.GetType().Name);
            throw;
        }
    }

    public async Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(objectKey)) {
            return new ImageObjectValidationResult(false, "invalid_key", "Image object key is required.");
        }

        try {
            StoredObjectInfo? info = await storageClient.GetObjectInfoAsync(_options.Bucket, objectKey, cancellationToken).ConfigureAwait(false);
            if (info is null) {
                return new ImageObjectValidationResult(false, "not_found", "Image upload has not completed.");
            }

            if (info.SizeBytes <= 0) {
                return new ImageObjectValidationResult(false, "empty", "Image file is empty.");
            }

            if (info.SizeBytes > _options.MaxUploadSizeBytes) {
                return new ImageObjectValidationResult(false, "too_large",
                    $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes.");
            }

            if (string.IsNullOrWhiteSpace(info.ContentType) || !AllowedContentTypes.Contains(info.ContentType)) {
                return new ImageObjectValidationResult(false, "unsupported_type",
                    $"Unsupported content type: {info.ContentType ?? "unknown"}.");
            }

            return new ImageObjectValidationResult(true);
        } catch (Exception ex) {
            IntegrationsTelemetry.RecordStorageOperation("head", "failure", ex.GetType().Name);
            throw;
        }
    }

    private static string NormalizeFileName(string fileName) {
        string nameOnly = Path.GetFileName(fileName);
        string cleaned = nameOnly.Replace(' ', '-');
        return cleaned.Length switch {
            0 => "image",
            > 128 => cleaned[..128],
            _ => cleaned
        };
    }

    private string BuildPublicUrl(string key) {
        string escapedKey = string.Join('/', key
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl)) {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{escapedKey}";
        }

        return !string.IsNullOrWhiteSpace(_options.ServiceUrl)
            ? $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.Bucket}/{escapedKey}"
            : $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{escapedKey}";
    }
}
