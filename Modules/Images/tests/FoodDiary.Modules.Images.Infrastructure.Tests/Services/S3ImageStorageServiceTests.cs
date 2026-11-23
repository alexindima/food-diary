using System.Reflection;
using MsOptions = Microsoft.Extensions.Options.Options;
using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Infrastructure.Providers.Services;
using FoodDiary.Modules.Images.Infrastructure.Providers.Options;
using System.Diagnostics.Metrics;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using SkiaSharp;

namespace FoodDiary.Modules.Images.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class S3ImageStorageServiceTests {
    private const string IntegrationsMeterName = "FoodDiary.Integrations";

    [Fact]
    public async Task PrivateConfirmationStoresSanitizedBytesUnderAnUnsignableStagingKeyUntilPromotionAsync() {
        byte[] raw = CreateImageBytes(SKEncodedImageFormat.Png);
        var storage = new StubObjectStorageClient(content: raw);
        S3ImageStorageService service = CreateService(storage);
        var key = ObjectStorageKey.FromStoredValue("users/test/recipe.png");

        ImageObjectValidationResult confirmation = await service.ConfirmPrivateUploadedObjectAsync(key, CancellationToken.None);

        Assert.True(confirmation.IsValid);
        byte[] approvedBytes = Assert.IsType<byte[]>(storage.PublishedContent);
        Assert.Multiple(
            () => Assert.Equal(("fooddiary-assets-staging", key.Value), storage.Read),
            () => Assert.Equal("fooddiary-assets-staging", storage.PublishedBucket),
            () => Assert.Equal("private-confirmed/users/test/recipe.png", storage.StoredKey));

        Array.Clear(raw);
        ImageObjectValidationResult promotion = await service.PromotePrivateImageAsync(key, CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(promotion.IsValid),
            () => Assert.Equal(("fooddiary-assets-staging", "private-confirmed/users/test/recipe.png"), storage.Read),
            () => Assert.Equal("fooddiary-assets", storage.PublishedBucket),
            () => Assert.Equal(key.Value, storage.StoredKey),
            () => Assert.Equal(approvedBytes, storage.PublishedContent));
    }

    [Theory]
    [InlineData("text/plain", false)]
    [InlineData("image/png", true)]
    public async Task PromotePrivateImage_RejectsUnsupportedMetadataOrMismatchedLengthBeforePublishingAsync(
        string contentType, bool readsContent) {
        var storage = new StubObjectStorageClient(new StoredObjectInfo(4, contentType), [1, 2, 3]);
        S3ImageStorageService service = CreateService(storage);
        var key = ObjectStorageKey.FromStoredValue("users/test/recipe.png");

        ImageObjectValidationResult promotion = await service.PromotePrivateImageAsync(key, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal("invalid_private_image", promotion.ErrorCode),
            () => Assert.Equal(readsContent ? ("fooddiary-assets-staging", key.ForPrivateConfirmedImage().Value) : null, storage.Read),
            () => Assert.Null(storage.PublishedContent));
    }

    [Fact]
    public void ContentTypeMatchesFormat_WhenCodecFormatIsUnsupported_ReturnsFalse() {
        MethodInfo method = typeof(S3ImageStorageService).GetMethod(
            "ContentTypeMatchesFormat",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        bool result = (bool)method.Invoke(null, ["image/png", SKEncodedImageFormat.Bmp])!;

        Assert.False(result);
    }

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

        var storageClient = new StubObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        PresignedUpload result = await service.CreatePresignedUploadAsync(
            UserId.New(),
            "meal.webp",
            "image/webp",
            1024,
            CancellationToken.None);

        Assert.NotNull(result.UploadUrl.Value);
        Assert.Contains("fooddiary-assets-staging", result.UploadUrl.Value, StringComparison.Ordinal);
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

        var storageClient = new StubObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

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

        Assert.Contains("meal-%231%3F.webp", result.FileUrl.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("meal-#1?.webp", result.FileUrl.Value, StringComparison.Ordinal);
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

        Assert.EndsWith("-image", result.ObjectKey.Value, StringComparison.Ordinal);
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

        string storedName = result.ObjectKey.Value.Split('-').Last();
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

        Assert.StartsWith("https://cdn.example.com/assets/users/", result.FileUrl.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectKeyBlank_ReturnsWithoutCallingStorage() {
        var storageClient = new CountingObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        await service.DeleteAsync(ObjectStorageKey.FromStoredValue("   "), CancellationToken.None);

        Assert.Equal(0, storageClient.DeleteCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerCancelsBlankDelete_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var storageClient = new CountingObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeleteAsync(ObjectStorageKey.FromStoredValue("   "), cancellationTokenSource.Token));

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

        await service.DeleteAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None);

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
            service.DeleteAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None));

        Assert.Equal(1, count);
        Assert.Equal("delete", operation);
        Assert.Equal("failure", outcome);
    }

    [Fact]
    public async Task UnconfiguredStorage_DeleteAsync_WhenObjectKeyBlank_Completes() {
        var service = new UnconfiguredImageStorageService();

        await service.DeleteAsync(ObjectStorageKey.FromStoredValue("   "), CancellationToken.None);
    }

    [Fact]
    public async Task UnconfiguredStorage_DeleteAsync_WhenObjectKeyIsPresent_FailsClosed() {
        var service = new UnconfiguredImageStorageService();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None));

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
        var storageClient = new StubObjectStorageClient();
        S3ImageStorageService service = CreateService(storageClient);

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsValid),
            () => Assert.Equal(1, count),
            () => Assert.Equal("validate", operation),
            () => Assert.Equal("success", outcome),
            () => Assert.Equal("fooddiary-assets", storageClient.PublishedBucket),
            () => Assert.Equal(CreateImageBytes(SKEncodedImageFormat.Png), storageClient.PublishedContent));
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenCallerCancelsBlankValidation_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("   "), cancellationTokenSource.Token));
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

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image"), CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenGifDecodes_ReturnsValid() {
        byte[] content = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/gif"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.gif"), CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ConfirmUploadedObjectAsync_RemovesJpegExifAndKeepsDisplayOrientation() {
        using var bitmap = new SKBitmap(12, 8);
        bitmap.Erase(SKColors.DarkBlue);
        using SKData jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);
        byte[] original = jpeg.ToArray();
        // A little-endian TIFF directory with the standard EXIF orientation tag (rotate 90 degrees).
        byte[] exif = Convert.FromHexString("FFE1002245786966000049492A0008000000010012010300010000000600000000000000");
        byte[] photo = [.. original.AsSpan(0, 2), .. exif, .. original.AsSpan(2)];
        var storage = new StubObjectStorageClient(new StoredObjectInfo(photo.Length, "image/jpeg"), photo);

        ImageObjectValidationResult result = await CreateService(storage).ConfirmUploadedObjectAsync(
            ObjectStorageKey.FromStoredValue("users/test/photo.jpg"), CancellationToken.None);

        Assert.True(result.IsValid);
        byte[] published = Assert.IsType<byte[]>(storage.PublishedContent);
        Assert.Equal(-1, published.AsSpan().IndexOf("Exif\0\0"u8));
        using var stream = new SKMemoryStream(published);
        using var product = SKCodec.Create(stream);
        Assert.NotNull(product);
        Assert.Equal(new SKSizeI(8, 12), product.Info.Size);
    }

    [Theory]
    [InlineData("image/png", "iVBORw0KGgoAAAANSUhEUgAAAAQAAAADCAIAAAA7ljmRAAAAHHRFWHRDb21tZW50AG9yaWdpbmFsLWNhbWVyYS1ub3RlcfK0pAAAADBlWElmTU0AKgAAAAgAAQEOAAIAAAAVAAAAGgAAAABvcmlnaW5hbC1jYW1lcmEtbm90ZQAAQ76rPgAAABNJREFUeJxjZGBoYIABJjgLnQMAEpwAhnB/lqAAAAAASUVORK5CYII=", 1)]
    [InlineData("image/webp", "UklGRsIAAABXRUJQVlA4WAoAAAAKAAAAAwAAAgAAQU5JTQYAAAAAAAAAAgBBTk1GKAAAAAAAAAAAAAMAAAIAAEYAAAJWUDhMDwAAAC8DgAAABxBRwP4HIqL/AQBBTk1GKAAAAAAAAAAAAAMAAAIAAIIAAABWUDhMDwAAAC8DgAAABxAFjP4HIqL/AQBFWElGNgAAAEV4aWYAAE1NACoAAAAIAAEBDgACAAAAFQAAABoAAAAAb3JpZ2luYWwtY2FtZXJhLW5vdGUAAA==", 2)]
    [InlineData("image/gif", "R0lGODlhBAADAIEAAAAAgAAAAAAAAAAAACH/C05FVFNDQVBFMi4wAwECAAAh/hRvcmlnaW5hbC1jYW1lcmEtbm90ZQAh+QQABwAAACwAAAAABAADAAAICAABCBxIUGBAACH5BAENAAEALAAAAAAEAAMAgYAAAAAAAAAAAAAAAAgIAAEIHEhQYEAAOw==", 2)]
    public async Task ConfirmUploadedObjectAsync_DropsOptionalMetadataAndKeepsFrames(string contentType, string fixture, int frames) {
        byte[] source = Convert.FromBase64String(fixture);
        Assert.True(source.AsSpan().IndexOf("original-camera-note"u8) >= 0);
        var storage = new StubObjectStorageClient(new StoredObjectInfo(source.Length, contentType), source);

        ImageObjectValidationResult result = await CreateService(storage).ConfirmUploadedObjectAsync(
            ObjectStorageKey.FromStoredValue("users/test/photo"), CancellationToken.None);

        Assert.True(result.IsValid);
        byte[] published = Assert.IsType<byte[]>(storage.PublishedContent);
        Assert.Equal(-1, published.AsSpan().IndexOf("original-camera-note"u8));
        using var stream = new SKMemoryStream(published);
        using var product = SKCodec.Create(stream);
        Assert.NotNull(product);
        Assert.Equal(new SKSizeI(4, 3), product.Info.Size);
        Assert.Equal(frames, Math.Max(1, product.FrameCount));
        if (frames > 1) {
            using var originalStream = new SKMemoryStream(source);
            using var original = SKCodec.Create(originalStream);
            Assert.NotNull(original);
            Assert.Equal(original.RepetitionCount, product.RepetitionCount);
            Assert.Equal(new[] { 70, 130 }, product.FrameInfo.Select(frame => frame.Duration));
        }
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenContentIsNotAnImage_ReturnsInvalidContent() {
        byte[] content = "not-an-image"u8.ToArray();
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/png"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.png"), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_content", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenMimeTypeDoesNotMatchContent_ReturnsInvalidContent() {
        byte[] content = CreateImageBytes(SKEncodedImageFormat.Png);
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(
            new StoredObjectInfo(content.LongLength, "image/jpeg"),
            content));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.jpg"), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_content", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectKeyBlank_ReturnsInvalidKey() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient());

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("   "), CancellationToken.None);

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

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None);

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
            ObjectStorageKey.FromStoredValue("users/test/image.webp"),
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectIsEmpty_ReturnsEmpty() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(0, "image/webp")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("empty", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenObjectIsTooLarge_ReturnsInvalid() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(6 * 1024 * 1024, "image/webp")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("too_large", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateUploadedObjectAsync_WhenContentTypeIsUnsupported_ReturnsInvalid() {
        S3ImageStorageService service = CreateService(new StubObjectStorageClient(new StoredObjectInfo(1024, "text/plain")));

        ImageObjectValidationResult result = await service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.txt"), CancellationToken.None);

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
            service.ValidateUploadedObjectAsync(ObjectStorageKey.FromStoredValue("users/test/image.webp"), CancellationToken.None));

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
            ObjectStorageKey.FromStoredValue("users/test/image.webp"),
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
                ObjectStorageKey.FromStoredValue("users/test/image.webp"),
                cancellationTokenSource.Token));
    }

    private static S3ImageStorageService CreateService(IObjectStorageClient storageClient, string publicBaseUrl = "") {
        return new S3ImageStorageService(
            storageClient,
            MsOptions.Create(new S3Options {
                Bucket = "fooddiary-assets",
                StagingBucket = "fooddiary-assets-staging",
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
        public string? PublishedBucket { get; private set; }
        public string? StoredKey { get; private set; }
        public byte[]? PublishedContent { get; private set; }
        public (string Bucket, string Key)? Read { get; private set; }

        public string GetPreSignedUploadUrl(
            string bucketName,
            string key,
            string contentType,
            long contentLength,
            DateTime expiresAt) =>
            $"https://storage.example.com/{bucketName}/{key}";

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) {
            PublishedBucket = bucketName;
            StoredKey = key;
            PublishedContent = content.ToArray();
            return Task.CompletedTask;
        }

        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObjectInfo?>(objectInfo ?? new StoredObjectInfo(GetContent(bucketName, key).LongLength, "image/png"));

        public Task<byte[]?> GetObjectBytesAsync(
            string bucketName,
            string key,
            long maximumBytes,
            CancellationToken cancellationToken) {
            Read = (bucketName, key);
            return Task.FromResult<byte[]?>(GetContent(bucketName, key));
        }

        private byte[] GetContent(string bucketName, string key) =>
            string.Equals(bucketName, PublishedBucket, StringComparison.Ordinal) && string.Equals(key, StoredKey, StringComparison.Ordinal)
                ? PublishedContent ?? _content
                : _content;
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

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            Task.CompletedTask;

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

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
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

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
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

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
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

        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
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
