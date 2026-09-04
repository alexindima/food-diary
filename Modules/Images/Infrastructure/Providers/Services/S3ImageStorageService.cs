using System.Globalization;
using System.Net.Mime;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace FoodDiary.Integrations.Services;

public sealed class S3ImageStorageService(
    IObjectStorageClient storageClient,
    IOptions<S3Options> options,
    TimeProvider dateTimeProvider) : IImageStorageService {
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) {
        MediaTypeNames.Image.Jpeg,
        "image/png",
        "image/webp",
        "image/gif",
    };
    private const int MaximumDecodedDimension = 2048;

    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            if (fileSizeBytes <= 0) {
                throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");
            }

            if (fileSizeBytes > _options.MaxUploadSizeBytes) {
                throw new InvalidOperationException(
                    string.Create(CultureInfo.InvariantCulture, $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes."));
            }

            if (!AllowedContentTypes.Contains(contentType)) {
                throw new InvalidOperationException($"Unsupported content type: {contentType}.");
            }

            string normalizedName = NormalizeFileName(fileName);
            string key = $"users/{userId.Value:D}/images/{Guid.NewGuid():N}-{normalizedName}";

            DateTime expiresAt = dateTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(15);
            string uploadUrl = storageClient.GetPreSignedUploadUrl(
                _options.StagingBucket,
                key,
                contentType,
                fileSizeBytes,
                expiresAt);
            string fileUrl = BuildPublicUrl(key);

            IntegrationsTelemetry.RecordStorageOperation("presign", "success");
            var result = new PresignedUpload(uploadUrl, fileUrl, key, expiresAt);
            return Task.FromResult(result);
        } catch (Exception ex) {
            string outcome;
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested) {
                outcome = "canceled";
            } else {
                outcome = ex is ArgumentException or InvalidOperationException ? "validation_error" : "failure";
            }

            IntegrationsTelemetry.RecordStorageOperation(
                "presign",
                outcome,
                ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(objectKey)) {
                return;
            }

            string bucket = isConfirmed ? _options.Bucket : _options.StagingBucket;
            await storageClient.DeleteObjectAsync(bucket, objectKey, cancellationToken).ConfigureAwait(false);
            IntegrationsTelemetry.RecordStorageOperation("delete", "success");
        } catch (Exception ex) {
            string outcome = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "canceled"
                : "failure";
            IntegrationsTelemetry.RecordStorageOperation("delete", outcome, ex.GetType().Name);
            throw;
        }
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        DeleteAsync(objectKey, isConfirmed: true, cancellationToken);

    public async Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) {
        string outcome = "success";
        string? errorType = null;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(objectKey)) {
                return CreateInvalidValidationResult(ref outcome, "invalid_key", "Image object key is required.");
            }

            StoredObjectInfo? info = await storageClient.GetObjectInfoAsync(_options.StagingBucket, objectKey, cancellationToken).ConfigureAwait(false);
            if (info is null) {
                return CreateInvalidValidationResult(ref outcome, "not_found", "Image upload has not completed.");
            }

            if (info.SizeBytes <= 0) {
                return CreateInvalidValidationResult(ref outcome, "empty", "Image file is empty.");
            }

            if (info.SizeBytes > _options.MaxUploadSizeBytes) {
                return CreateInvalidValidationResult(ref outcome, "too_large",
                    string.Create(CultureInfo.InvariantCulture, $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes."));
            }

            if (string.IsNullOrWhiteSpace(info.ContentType) || !AllowedContentTypes.Contains(info.ContentType)) {
                return CreateInvalidValidationResult(ref outcome, "unsupported_type",
                    $"Unsupported content type: {info.ContentType ?? "unknown"}.");
            }

            byte[]? content = await storageClient.GetObjectBytesAsync(
                _options.StagingBucket,
                objectKey,
                _options.MaxUploadSizeBytes,
                cancellationToken).ConfigureAwait(false);
            if (content is null) {
                return CreateInvalidValidationResult(ref outcome, "not_found", "Image upload has not completed.");
            }

            if (content.LongLength != info.SizeBytes || !HasValidImageContent(content, info.ContentType)) {
                return CreateInvalidValidationResult(ref outcome, "invalid_content",
                    "Uploaded object content does not match a supported image format.");
            }

            await PublishValidatedContentAsync(objectKey, info.ContentType, content, cancellationToken).ConfigureAwait(false);

            return new ImageObjectValidationResult(IsValid: true);
        } catch (InvalidDataException ex) {
            errorType = ex.GetType().Name;
            return CreateInvalidValidationResult(ref outcome, "invalid_content",
                "Uploaded object content does not match a supported image format.");
        } catch (Exception ex) {
            outcome = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "canceled"
                : "failure";
            errorType = ex.GetType().Name;
            throw;
        } finally {
            IntegrationsTelemetry.RecordStorageOperation("validate", outcome, errorType);
        }
    }

    public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) =>
        ConfirmUploadedObjectAsync(objectKey, cancellationToken);

    private Task PublishValidatedContentAsync(
        string objectKey,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken) =>
        storageClient.PutObjectBytesAsync(
            _options.Bucket,
            objectKey,
            contentType,
            content,
            cancellationToken);

    private static ImageObjectValidationResult CreateInvalidValidationResult(
        ref string outcome,
        string errorCode,
        string errorMessage) {
        outcome = errorCode;
        return new ImageObjectValidationResult(IsValid: false, errorCode, errorMessage);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static bool HasValidImageContent(byte[] content, string contentType) {
        using var stream = new SKMemoryStream(content);
        using var codec = SKCodec.Create(stream);
        if (codec is null || !ContentTypeMatchesFormat(contentType, codec.EncodedFormat)) {
            return false;
        }

        SKImageInfo original = codec.Info;
        if (original.Width <= 0 || original.Height <= 0) {
            return false;
        }

        double scale = Math.Min(1d, MaximumDecodedDimension / (double)Math.Max(original.Width, original.Height));
        var decoded = new SKImageInfo(
            Math.Max(1, (int)Math.Round(original.Width * scale, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)Math.Round(original.Height * scale, MidpointRounding.AwayFromZero)),
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using var bitmap = new SKBitmap(decoded);
        return codec.GetPixels(decoded, bitmap.GetPixels()) == SKCodecResult.Success;
    }

    private static bool ContentTypeMatchesFormat(string contentType, SKEncodedImageFormat format) {
        return format switch {
            SKEncodedImageFormat.Jpeg => string.Equals(contentType, MediaTypeNames.Image.Jpeg, StringComparison.OrdinalIgnoreCase),
            SKEncodedImageFormat.Png => string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase),
            SKEncodedImageFormat.Webp => string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase),
            SKEncodedImageFormat.Gif => string.Equals(contentType, "image/gif", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static string NormalizeFileName(string fileName) {
        string nameOnly = Path.GetFileName(fileName);
        string cleaned = nameOnly.Replace(' ', '-');
        return cleaned.Length switch {
            0 => "image",
            > 128 => cleaned[..128],
            _ => cleaned,
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
