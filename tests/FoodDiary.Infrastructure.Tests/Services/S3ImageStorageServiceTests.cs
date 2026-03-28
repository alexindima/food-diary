using System.Diagnostics.Metrics;
using Amazon.S3.Model;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class S3ImageStorageServiceTests {
    private const string InfrastructureMeterName = "FoodDiary.Infrastructure";

    [Fact]
    public async Task CreatePresignedUploadAsync_WhenInputIsValid_RecordsSuccessMetric() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using var listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });

        var service = CreateService(new StubObjectStorageClient());

        var result = await service.CreatePresignedUploadAsync(
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
        using var listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
            errorType = GetTagValue(tags, "error.type");
        });

        var service = CreateService(new StubObjectStorageClient());

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
    public async Task DeleteAsync_WhenTransportFails_RecordsFailureMetric() {
        long? count = null;
        string? operation = null;
        string? outcome = null;
        using var listener = CreateInfrastructureListener((value, tags) => {
            count = value;
            operation = GetTagValue(tags, "fooddiary.storage.operation");
            outcome = GetTagValue(tags, "fooddiary.storage.outcome");
        });

        var service = CreateService(new ThrowingObjectStorageClient(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("users/test/image.webp", CancellationToken.None));

        Assert.Equal(1, count);
        Assert.Equal("delete", operation);
        Assert.Equal("failure", outcome);
    }

    private static S3ImageStorageService CreateService(IObjectStorageClient storageClient) {
        return new S3ImageStorageService(
            storageClient,
            Microsoft.Extensions.Options.Options.Create(new S3Options {
                Bucket = "fooddiary-assets",
                Region = "eu-central-1",
                MaxUploadSizeBytes = 5 * 1024 * 1024
            }),
            new StubDateTimeProvider());
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onOperation) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != InfrastructureMeterName) {
                return;
            }

            if (instrument.Name == "fooddiary.storage.operations") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (instrument.Name == "fooddiary.storage.operations") {
                onOperation(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private sealed class StubObjectStorageClient : IObjectStorageClient {
        public string GetPreSignedUrl(GetPreSignedUrlRequest request) =>
            $"https://storage.example.com/{request.BucketName}/{request.Key}";

        public Task DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingObjectStorageClient(Exception exception) : IObjectStorageClient {
        public string GetPreSignedUrl(GetPreSignedUrlRequest request) => throw exception;

        public Task DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken) =>
            Task.FromException(exception);
    }

    private sealed class StubDateTimeProvider : FoodDiary.Application.Common.Interfaces.Services.IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
    }
}
