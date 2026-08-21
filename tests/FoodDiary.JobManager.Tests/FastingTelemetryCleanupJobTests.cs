using FoodDiary.Application.Fasting.Services;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryCleanupJobTests : IDisposable {
    private readonly JobExecutionStateTracker _stateTracker = new();

    [Fact]
    public async Task Execute_WhenDisabled_DoesNotRunCleanup() {
        var cleanupService = new RecordingFastingTelemetryCleanupService();
        FastingTelemetryCleanupJob job = CreateJob(
            cleanupService,
            new FastingTelemetryCleanupOptions { Enabled = false });

        await job.Execute();

        Assert.Equal(0, cleanupService.CallCount);
        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("fasting.telemetry_cleanup");
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
    }

    [Fact]
    public async Task Execute_WhenEnabled_UsesConfiguredCutoffAndBatchSize() {
        var cleanupService = new RecordingFastingTelemetryCleanupService(result: 7);
        var nowUtc = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc);
        FastingTelemetryCleanupJob job = CreateJob(
            cleanupService,
            new FastingTelemetryCleanupOptions {
                Enabled = true,
                RetentionDays = 90,
                BatchSize = 25,
            },
            new FixedTimeProvider(nowUtc));

        await job.Execute();

        Assert.Multiple(
            () => Assert.Equal(1, cleanupService.CallCount),
            () => Assert.Equal(nowUtc.AddDays(-90), cleanupService.LastCutoffUtc),
            () => Assert.Equal(25, cleanupService.LastBatchSize));
        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("fasting.telemetry_cleanup");
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WhenCleanupThrows_RecordsFailureAndRethrows() {
        var cleanupService = new RecordingFastingTelemetryCleanupService(throwOnCleanup: true);
        FastingTelemetryCleanupJob job = CreateJob(cleanupService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("fasting.telemetry_cleanup");
        Assert.Equal(1, snapshot!.Value.ConsecutiveFailures);
    }

    [Fact]
    public async Task Execute_WhenCanceled_RecordsCancellationAndRethrows() {
        var cleanupService = new RecordingFastingTelemetryCleanupService();
        FastingTelemetryCleanupJob job = CreateJob(cleanupService);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellation.Token));

        JobExecutionStateSnapshot? snapshot = _stateTracker.GetSnapshot("fasting.telemetry_cleanup");
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
    }

    private FastingTelemetryCleanupJob CreateJob(
        IFastingTelemetryCleanupService cleanupService,
        FastingTelemetryCleanupOptions? options = null,
        TimeProvider? timeProvider = null) =>
        new(
            cleanupService,
            Options.Create(options ?? new FastingTelemetryCleanupOptions()),
            new JobExecutionObserver(
                timeProvider ?? new FixedTimeProvider(new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc)),
                _stateTracker),
            NullLogger<FastingTelemetryCleanupJob>.Instance);

    public void Dispose() => _stateTracker.Dispose();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingFastingTelemetryCleanupService(
        int result = 0,
        bool throwOnCleanup = false) : IFastingTelemetryCleanupService {
        public int CallCount { get; private set; }
        public DateTime? LastCutoffUtc { get; private set; }
        public int? LastBatchSize { get; private set; }

        public Task<int> CleanupAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken) {
            CallCount++;
            LastCutoffUtc = olderThanUtc;
            LastBatchSize = batchSize;
            return throwOnCleanup
                ? throw new InvalidOperationException("cleanup failed")
                : Task.FromResult(result);
        }
    }
}
