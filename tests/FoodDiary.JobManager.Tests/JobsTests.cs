using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

public sealed class JobsTests {
    [Fact]
    public async Task ImageCleanupJob_WithNonPositiveBatchSize_UsesOne() {
        var cleanupService = new RecordingImageCleanupService([1, 0]);
        var options = Options.Create(new ImageCleanupOptions { BatchSize = 0, OlderThanHours = 12 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new ImageCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            NullLogger<ImageCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal([1, 1], cleanupService.BatchSizes);
        Assert.Equal(now.AddHours(-12), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task ImageCleanupJob_WithNonPositiveOlderThan_UsesDefault12Hours() {
        var cleanupService = new RecordingImageCleanupService([0]);
        var options = Options.Create(new ImageCleanupOptions { BatchSize = 10, OlderThanHours = 0 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new ImageCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            NullLogger<ImageCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.OlderThanValues);
        Assert.Equal(now.AddHours(-12), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithInvalidReassignUserId_PassesNull() {
        var cleanupService = new RecordingUserCleanupService([0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 10,
            RetentionDays = 30,
            ReassignUserId = "not-a-guid",
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.ReassignUserIds);
        Assert.Null(cleanupService.ReassignUserIds[0]);
        Assert.Equal(now.AddDays(-30), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithValidReassignUserId_PassesParsedGuid() {
        var expectedId = Guid.NewGuid();
        var cleanupService = new RecordingUserCleanupService([0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 10,
            RetentionDays = 30,
            ReassignUserId = expectedId.ToString(),
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.ReassignUserIds);
        Assert.Equal(expectedId, cleanupService.ReassignUserIds[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithNonPositiveBatchAndRetention_UsesDefaults() {
        var cleanupService = new RecordingUserCleanupService([1, 0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 0,
            RetentionDays = 0,
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal([1, 1], cleanupService.BatchSizes);
        Assert.Equal(now.AddDays(-30), cleanupService.OlderThanValues[0]);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class RecordingImageCleanupService(IEnumerable<int> results) : IImageAssetCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<int> BatchSizes { get; } = [];
        public List<DateTime> OlderThanValues { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(Domain.ValueObjects.Ids.ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(false));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
            OlderThanValues.Add(olderThanUtc);
            BatchSizes.Add(batchSize);
            var value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    private sealed class RecordingUserCleanupService(IEnumerable<int> results) : IUserCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<int> BatchSizes { get; } = [];
        public List<DateTime> OlderThanValues { get; } = [];
        public List<Guid?> ReassignUserIds { get; } = [];

        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc,
            int batchSize,
            Guid? reassignUserId,
            CancellationToken cancellationToken = default) {
            OlderThanValues.Add(olderThanUtc);
            BatchSizes.Add(batchSize);
            ReassignUserIds.Add(reassignUserId);
            var value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }
}
