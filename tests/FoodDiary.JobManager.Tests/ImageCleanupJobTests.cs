using FoodDiary.Mediator;
using FoodDiary.Testing;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageCleanupJobTests : IDisposable {
    private readonly JobExecutionStateTracker _stateTracker = new();

    [Fact]
    public async Task Execute_WhenNoOrphans_RecordsSuccess() {
        var cleanup = new StubImageCleanupService(totalAvailable: 0);
        ImageCleanupJob job = CreateJob(cleanup);

        await job.Execute();

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("images.cleanup");
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
        Assert.NotNull(snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WithOrphans_DispatchesOneBoundedCleanupRequest() {
        var cleanup = new StubImageCleanupService(totalAvailable: 5);
        var options = new ImageCleanupOptions { BatchSize = 3, OlderThanHours = 12 };
        ImageCleanupJob job = CreateJob(cleanup, options);

        await job.Execute();

        Assert.Equal(5, cleanup.TotalDeleted);
        Assert.Equal(1, cleanup.CallCount);
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
        IRequestHandler<CleanupOrphanImagesCommand, int> cleanupService,
        ImageCleanupOptions? options = null) {
        return new ImageCleanupJob(
            RequestTestSender.Create(cleanupService),
            Options.Create(options ?? new ImageCleanupOptions()),
            new JobExecutionObserver(new FixedDateTimeProvider(), _stateTracker),
            NullLogger<ImageCleanupJob>.Instance);
    }

    public void Dispose() => _stateTracker.Dispose();

    [ExcludeFromCodeCoverage]
    private sealed class StubImageCleanupService(int totalAvailable) : IRequestHandler<CleanupOrphanImagesCommand, int> {
        public int TotalDeleted { get; private set; }
        public int CallCount { get; private set; }

        public Task<int> Handle(CleanupOrphanImagesCommand request, CancellationToken cancellationToken) {
            CallCount++;
            int toDelete = totalAvailable - TotalDeleted;
            TotalDeleted += toDelete;
            return Task.FromResult(toDelete);
        }

    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageCleanupService : IRequestHandler<CleanupOrphanImagesCommand, int> {
        public Task<int> Handle(CleanupOrphanImagesCommand request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("S3 error");

    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
    }
}
