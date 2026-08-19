using System.Diagnostics.Metrics;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using SkiaSharp;

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
    public async Task CreatePresignedUploadAsync_WhenCallerCancels_DoesNotGenerateUploadUrl() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var storageClient = new CountingObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CreatePresignedUploadAsync(
                UserId.New(),
                "meal.webp",
                "image/webp",
                1024,
                cancellationTokenSource.Token));

        Assert.Equal(0, storageClient.PresignCount);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreatePresignedUploadAsync_WhenFileSizeIsNotPositive_ThrowsOutOfRange(long fileSizeBytes) {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreatePresignedUploadAsync(
                UserId.New(),
                "meal.webp",
                "image/webp",
                fileSizeBytes,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreatePresignedUploadAsync_WhenFileTooLarge_ThrowsInvalidOperationException() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePresignedUploadAsync(
                UserId.New(),
                "meal.webp",
                "image/webp",
                6 * 1024 * 1024,
                CancellationToken.None));
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
    public async Task CreatePresignedUploadAsync_WithPathOnlyFileName_UsesImageFallbackName() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            "/",
            "image/webp",
            1024,
            CancellationToken.None);

        Assert.EndsWith("-image", result.ObjectKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreatePresignedUploadAsync_WithVeryLongFileName_TruncatesName() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());
        string longName = new('a', 140);

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            longName,
            "image/webp",
            1024,
            CancellationToken.None);

        string storedName = result.ObjectKey.Split('-').Last();
        Assert.Equal(128, storedName.Length);
    }

    [Fact]
    public async Task CreatePresignedUploadAsync_WithPublicBaseUrl_UsesPublicBaseUrl() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(), publicBaseUrl: "https://cdn.example.com/assets/");

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            "meal.webp",
            "image/webp",
            1024,
            CancellationToken.None);

        Assert.StartsWith("https://cdn.example.com/assets/users/", result.FileUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectKeyBlank_ReturnsWithoutCallingStorage() {
        var storageClient = new CountingObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        await service.DeleteAsync("   ", CancellationToken.None);

        Assert.Equal(0, storageClient.DeleteCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerCancelsBlankDelete_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var storageClient = new CountingObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeleteAsync("   ", cancellationTokenSource.Token));

        Assert.Equal(0, storageClient.DeleteCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectDeleted_RecordsSuccessMetric() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });

        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await service.DeleteAsync("users/test/image.webp", CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal("delete", operation);
        Assert.Equal("success", outcome);
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
    public async Task UnconfiguredStorage_DeleteAsync_WhenObjectKeyBlank_Completes() {
        var service = new UnconfiguredImageStorageService();

        await service.DeleteAsync("   ", CancellationToken.None);
    }

    [Fact]
    public async Task UnconfiguredStorage_DeleteAsync_WhenObjectKeyIsPresent_FailsClosed() {
        var service = new UnconfiguredImageStorageService();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("users/test/image.webp", CancellationToken.None));

        Assert.Equal("Image storage is not configured.", exception.Message);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectMetadataIsValid_ReturnsValid() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsValid),
            () => Assert.Equal(1, count),
            () => Assert.Equal("validate", operation),
            () => Assert.Equal("success", outcome));
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenCallerCancelsBlankValidation_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ValidateUploadedObjectAsync("   ", cancellationTokenSource.Token));
    }

    [Theory]
    [InlineData("image/jpeg", SKEncodedImageFormat.Jpeg)]
    [InlineData("image/png", SKEncodedImageFormat.Png)]
    [InlineData("image/webp", SKEncodedImageFormat.Webp)]
    public async Task ValidateUploadedObjectAsync_WhenSupportedImageDecodes_ReturnsValid(
        string contentType,
        SKEncodedImageFormat format) {
        byte[] content = CreateImageBytes(format);
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, contentType),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image", CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenGifDecodes_ReturnsValid() {
        byte[] content = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/gif"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.gif", CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenContentIsNotAnImage_ReturnsInvalidContent() {
        byte[] content = "not-an-image"u8.ToArray();
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/png"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.png", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_content", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenMimeTypeDoesNotMatchContent_ReturnsInvalidContent() {
        byte[] content = CreateImageBytes(SKEncodedImageFormat.Png);
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/jpeg"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.jpg", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_content", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectKeyBlank_ReturnsInvalidKey() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("   ", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_key", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectInfoMissing_ReturnsNotFound() {
        string? operation = null;
        string? outcome = null;
        using MeterListener listener = CreateInfrastructureListener((_, tags) => {
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });
        S3ImageStorageService service = CreateService(new NullObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None);

        Assert.Multiple(
            () => Assert.False(result.IsValid),
            () => Assert.Equal("not_found", result.ErrorCode),
            () => Assert.Equal("validate", operation),
            () => Assert.Equal("not_found", outcome));
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectContentDisappears_ReturnsNotFound() {
        S3ImageStorageService service = CreateService(new MissingContentObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(
            "users/test/image.webp",
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectIsEmpty_ReturnsEmpty() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(0, "image/webp")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("empty", result.ErrorCode);
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

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenStorageThrows_Rethrows() {
        string? operation = null;
        string? outcome = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener((_, tags) => {
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
            errorType = GetTagValue(tags, "error.type");
        });
        S3ImageStorageService service = CreateService(new ThrowingObjectStorageClient(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidateUploadedObjectAsync("users/test/image.webp", CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal("validate", operation),
            () => Assert.Equal("failure", outcome),
            () => Assert.Equal(nameof(InvalidOperationException), errorType));
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenStorageReportsOversizedContent_ReturnsInvalidContent() {
        S3ImageStorageService service = CreateService(
            new ThrowingObjectStorageClient(new InvalidDataException("too large")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(
            "users/test/image.webp",
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_content", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        S3ImageStorageService service = CreateService(new CanceledObjectStorageClient());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ValidateUploadedObjectAsync(
                "users/test/image.webp",
                cancellationTokenSource.Token));
    }

    private static S3ImageStorageService CreateService(IObjectStorageClient storageClient, string publicBaseUrl = "") {
        return new S3ImageStorageService(
            storageClient,
            Microsoft.Extensions.Options.Options.Create(new S3Options {
                Bucket = "fooddiary-assets",
                Region = "eu-central-1",
                MaxUploadSizeBytes = 5 * 1024 * 1024,
                PublicBaseUrl = publicBaseUrl,
            }),
            new StubDateTimeProvider());
    }

    private static byte[] CreateImageBytes(SKEncodedImageFormat format) {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Green);
        using var image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, 90);
        return data.ToArray();
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
    private sealed class StubObjectStorageClient(
        StoredObjectInfo? objectInfo = null,
        byte[]? content = null) : IObjectStorageClient {
        private readonly byte[] _content = content ?? CreateImageBytes(SKEncodedImageFormat.Png);

        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) =>
            $"https://storage.example.com/{bucketName}/{key}";

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(objectInfo ?? new StoredObjectInfo(_content.LongLength, "image/png"));

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) => Task.FromResult<byte[]?>(_content);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingObjectStorageClient : IObjectStorageClient {
        public int PresignCount { get; private set; }
        public int DeleteCount { get; private set; }

        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) {
            PresignCount++;
            return $"https://storage.example.com/{bucketName}/{key}";
        }

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) {
            DeleteCount++;
            return Task.CompletedTask;
        }

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(new StoredObjectInfo(1024, "image/webp"));

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) => Task.FromResult<byte[]?>(CreateImageBytes(SKEncodedImageFormat.Webp));
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullObjectStorageClient : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) =>
            $"https://storage.example.com/{bucketName}/{key}";

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(null);

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) => Task.FromResult<byte[]?>(null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class MissingContentObjectStorageClient : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) => throw new NotSupportedException();

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StoredObjectInfo?> GetObjectInfoAsync(
            string bucketName,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(new StoredObjectInfo(1024, "image/webp"));

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) => Task.FromResult<byte[]?>(null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingObjectStorageClient(Exception exception) : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) => throw exception;

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromException(exception);

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromException<StoredObjectInfo?>(exception);

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) => Task.FromException<byte[]?>(exception);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CanceledObjectStorageClient : IObjectStorageClient {
        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) => throw new NotSupportedException();

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromCanceled(cancellationToken);

        public Task<StoredObjectInfo?> GetObjectInfoAsync(
            string bucketName,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromCanceled<StoredObjectInfo?>(cancellationToken);

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) =>
            Task.FromCanceled<byte[]?>(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() =>
            new(new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc));
    }
}
