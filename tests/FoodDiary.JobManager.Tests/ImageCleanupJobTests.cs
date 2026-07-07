using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageCleanupJobTests : IDisposable {
    private readonly JobExecutionStateTracker _stateTracker = new();

    [Fact]
    public async Task Execute_WhenNoOrphans_RecordsSuccess() {
        var cleanup = new StubImageCleanupService(itemsPerBatch: 0);
        ImageCleanupJob job = CreateJob(cleanup);

        await job.Execute();

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("images.cleanup");
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
        Assert.NotNull(snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WithOrphans_DeletesInBatches() {
        var cleanup = new StubImageCleanupService(itemsPerBatch: 3, totalAvailable: 5);
        var options = new ImageCleanupOptions { BatchSize = 3, OlderThanHours = 12 };
        ImageCleanupJob job = CreateJob(cleanup, options);

        await job.Execute();

        Assert.Equal(5, cleanup.TotalDeleted);
        Assert.Equal(2, cleanup.CallCount);
    }

    [Fact]
    public async Task Execute_WhenServiceThrows_RecordsFailureAndRethrows() {
        var cleanup = new ThrowingImageCleanupService();
        ImageCleanupJob job = CreateJob(cleanup);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("images.cleanup");
        Assert.Equal(1, snapshot!.Value.ConsecutiveFailures);
    }

    private ImageCleanupJob CreateJob(
        IImageAssetCleanupService cleanupService,
        ImageCleanupOptions? options = null) {
        return new ImageCleanupJob(
            cleanupService,
            Options.Create(options ?? new ImageCleanupOptions()),
            new JobExecutionObserver(new FixedDateTimeProvider(), _stateTracker),
            NullLogger<ImageCleanupJob>.Instance);
    }

    public void Dispose() => _stateTracker.Dispose();

    [ExcludeFromCodeCoverage]
    private sealed class StubImageCleanupService(int itemsPerBatch, int totalAvailable = 0) : IImageAssetCleanupService {
        public int TotalDeleted { get; private set; }
        public int CallCount { get; private set; }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken ct = default) {
            CallCount++;
            int remaining = totalAvailable - TotalDeleted;
            int toDelete = Math.Min(Math.Min(itemsPerBatch, batchSize), remaining);
            TotalDeleted += toDelete;
            return Task.FromResult(toDelete);
        }

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(
            Domain.ValueObjects.Ids.ImageAssetId assetId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageCleanupService : IImageAssetCleanupService {
        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken ct = default) =>
            throw new InvalidOperationException("S3 error");

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(
            Domain.ValueObjects.Ids.ImageAssetId assetId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
    }
}
