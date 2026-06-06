using System.Globalization;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class TestImageStorageService(IOptions<S3Options> options) : IImageStorageService {
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (fileSizeBytes <= 0) {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");
        }

        if (fileSizeBytes > _options.MaxUploadSizeBytes) {
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes."));
        }

        if (!AllowedContentTypes.Contains(contentType)) {
            throw new InvalidOperationException($"Unsupported content type: {contentType}.");
        }

        string safeFileName = NormalizeFileName(fileName);
        string objectKey = $"users/{userId.Value:D}/images/{Guid.NewGuid():N}-{safeFileName}";
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(15);
        string uploadUrl = $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.Bucket}/{objectKey}";

        return Task.FromResult(new PresignedUpload(
            uploadUrl,
            $"https://cdn.test.local/{objectKey}",
            objectKey,
            expiresAt));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        string objectKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ImageObjectValidationResult(IsValid: true));

    private static string NormalizeFileName(string fileName) {
        string nameOnly = Path.GetFileName(fileName);
        string cleaned = nameOnly.Replace(' ', '-');
        return cleaned.Length switch {
            0 => "image",
            > 128 => cleaned[..128],
            _ => cleaned,
        };
    }
}
