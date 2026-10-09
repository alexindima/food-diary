using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Infrastructure.Providers.Options;
using System.Globalization;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        string uploadUrl = $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.StagingBucket}/{objectKey}";

        return Task.FromResult(new PresignedUpload(
                SignedImageUploadUrl.FromProviderValue(uploadUrl),
                PublicImageUrl.FromProviderValue($"https://cdn.test.local/{objectKey}"),
                ObjectStorageKey.FromStoredValue(objectKey),
            expiresAt));
    }

    public Task DeleteAsync(ObjectStorageKey key, bool isConfirmed, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteAsync(ObjectStorageKey key, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<ImageObjectValidationResult> ConfirmUploadedObjectAsync(
        ObjectStorageKey key,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ImageObjectValidationResult(IsValid: true));

    public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
        ObjectStorageKey key,
        CancellationToken cancellationToken) =>
        ConfirmUploadedObjectAsync(key, cancellationToken);

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
