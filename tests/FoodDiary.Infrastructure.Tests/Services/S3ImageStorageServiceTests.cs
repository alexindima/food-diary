using System.Diagnostics.Metrics;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class S3ImageStorageServiceTests {
    private const string IntegrationsMeterName = "FoodDiary.Integrations";

    [Fact]
    public async Task CreatePresignedUploadAsync_WhenInputIsValid_RecordsSuccessMetric() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });

        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            "meal.webp",
            "image/webp",
            1024,
            CancellationToken.None);

        Assert.NotNull(result.UploadUrl);
        Assert.Equal(1, count);
        Assert.Equal("presign", operation);
        Assert.Equal("success", outcome);
    }

    [Fact]
    public async Task CreatePresignedUploadAsync_WhenContentTypeIsInvalid_RecordsValidationErrorMetric() {
        long? count = null;
        string? outcome = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
            errorType = GetTagValue(tags, "error.type");
        });

        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePresignedUploadAsync(
                UserId.New(),
                "meal.txt",
                "text/plain",
                128,
                CancellationToken.None));

        Assert.Equal(1, count);
        Assert.Equal("validation_error", outcome);
        Assert.Equal(nameof(InvalidOperationException), errorType);
    }

    [Fact]
    public async Task CreatePresignedUploadAsync_WithUnsafeFileName_EscapesPublicUrlKeySegments() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            "meal #1?.webp",
            "image/webp",
            1024,
            CancellationToken.None);

        Assert.Contains("meal-%231%3F.webp", result.FileUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("meal-#1?.webp", result.FileUrl, StringComparison.Ordinal);
    }


    [Fact]
    public async Task DeleteAsync_WhenTransportFails_RecordsFailureMetric() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });

        S3ImageStorageService service = CreateService(new ThrowingObjectStorageClient(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("users/test/image.webp", CancellationToken.None));

        Assert.Equal(1, count);
        Assert.Equal("delete", operation);
        Assert.Equal("failure", outcome);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectMetadataIsValid_ReturnsValid() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectIsTooLarge_ReturnsInvalid() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(6 * 1024 * 1024, "image/webp")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("too_large", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenContentTypeIsUnsupported_ReturnsInvalid() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(1024, "text/plain")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.txt", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("unsupported_type", result.ErrorCode);
    }

    private static S3ImageStorageService CreateService(IObjectStorageClient storageClient) {
        return new S3ImageStorageService(
            storageClient,
            Microsoft.Extensions.Options.Options.Create(new S3Options {
                Bucket = "fooddiary-assets",
                Region = "eu-central-1",
                MaxUploadSizeBytes = 5 * 1024 * 1024,
            }),
            new StubDateTimeProvider());
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onOperation) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, IntegrationsMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.storage.operations", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.storage.operations", StringComparison.Ordinal)) {
                onOperation(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubObjectStorageClient(StoredObjectInfo? objectInfo = null) : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            DateTime expiresAt) =>
            $"https://storage.example.com/{bucketName}/{key}";

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(objectInfo ?? new StoredObjectInfo(1024, "image/webp"));
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingObjectStorageClient(Exception exception) : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            DateTime expiresAt) => throw exception;

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromException(exception);

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromException<StoredObjectInfo?>(exception);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : FoodDiary.Application.Abstractions.Common.Interfaces.Services.IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
    }
}
