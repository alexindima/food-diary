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

            var normalizedName = NormalizeFileName(fileName);
            var key = $"users/{userId.Value:D}/images/{Guid.NewGuid():N}-{normalizedName}";

            var expiresAt = dateTimeProvider.UtcNow.AddMinutes(15);
            var uploadUrl = storageClient.GetPreSignedUploadUrl(_options.Bucket, key, contentType, expiresAt);
            var fileUrl = BuildPublicUrl(key);

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
            await storageClient.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
            IntegrationsTelemetry.RecordStorageOperation("delete", "success");
        } catch (Exception ex) {
            IntegrationsTelemetry.RecordStorageOperation("delete", "failure", ex.GetType().Name);
            throw;
        }
    }

    private static string NormalizeFileName(string fileName) {
        var nameOnly = Path.GetFileName(fileName);
        var cleaned = nameOnly.Replace(' ', '-');
        return cleaned.Length switch {
            0 => "image",
            > 128 => cleaned[..128],
            _ => cleaned
        };
    }

    private string BuildPublicUrl(string key) {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl)) {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
        }

        return !string.IsNullOrWhiteSpace(_options.ServiceUrl)
            ? $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.Bucket}/{key}"
            : $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{key}";
    }
}
