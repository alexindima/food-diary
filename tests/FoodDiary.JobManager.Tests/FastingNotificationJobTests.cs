using FoodDiary.Application.Fasting.Services;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingNotificationJobTests {
    private const string JobManagerMeterName = "FoodDiary.JobManager";

    [Fact]
    public async Task Execute_WhenDisabled_DoesNotProcessNotifications() {
        var scheduler = new RecordingFastingNotificationScheduler(3);
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new FastingNotificationJob(
            scheduler,
            Options.Create(new FastingNotificationOptions { Enabled = false }),
            new JobExecutionObserver(new FixedDateTimeProvider(now), tracker),
            NullLogger<FastingNotificationJob>.Instance);

        await job.Execute();

        Assert.Equal(0, scheduler.CallCount);
        Assert.Equal(0, tracker.GetSnapshot("fasting.notifications")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("fasting.notifications")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WhenEnabled_ProcessesNotificationsAndRecordsMetrics() {
        long? executionCount = null;
        string? outcome = null;
        long? processedItems = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onProcessedItems: (value, _) => processedItems = value,
            onDuration: (value, _) => duration = value);

        var scheduler = new RecordingFastingNotificationScheduler(3);
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new FastingNotificationJob(
            scheduler,
            Options.Create(new FastingNotificationOptions { Enabled = true }),
            new JobExecutionObserver(new FixedDateTimeProvider(now), tracker),
            NullLogger<FastingNotificationJob>.Instance);

        await job.Execute();

        Assert.Equal(1, scheduler.CallCount);
        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.Equal(3, processedItems);
        Assert.NotNull(duration);
        Assert.Equal(0, tracker.GetSnapshot("fasting.notifications")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("fasting.notifications")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WhenSchedulerFails_RecordsFailureAndRethrows() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onProcessedItems: null,
            onDuration: (value, _) => duration = value);

        var scheduler = new ThrowingFastingNotificationScheduler();
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var logger = new RecordingLogger<FastingNotificationJob>();
        var job = new FastingNotificationJob(
            scheduler,
            Options.Create(new FastingNotificationOptions { Enabled = true }),
            new JobExecutionObserver(new FixedDateTimeProvider(now), tracker),
            logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        Assert.Equal(1, scheduler.CallCount);
        Assert.Equal(1, executionCount);
        Assert.Equal("failure", outcome);
        Assert.NotNull(duration);
        Assert.Equal(1, tracker.GetSnapshot("fasting.notifications")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("fasting.notifications")?.LastFailedAtUtc);
        Assert.Equal(LogLevel.Error, logger.LastLogLevel);
    }

    [Fact]
    public async Task Execute_WhenCanceledBeforeWork_RecordsCanceledMetricAndRethrows() {
        long? executionCount = null;
        string? outcome = null;

        using MeterListener listener = CreateJobManagerListener(
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onProcessedItems: null,
            onDuration: null);

        var scheduler = new RecordingFastingNotificationScheduler(3);
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new FastingNotificationJob(
            scheduler,
            Options.Create(new FastingNotificationOptions { Enabled = true }),
            new JobExecutionObserver(new FixedDateTimeProvider(now), tracker),
            NullLogger<FastingNotificationJob>.Instance);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => job.Execute(cts.Token));

        Assert.Equal(0, scheduler.CallCount);
        Assert.Equal(1, executionCount);
        Assert.Equal("canceled", outcome);
        JobExecutionStateSnapshot? snapshot = tracker.GetSnapshot("fasting.notifications");
        Assert.NotNull(snapshot);
        Assert.Equal(now, snapshot.Value.LastStartedAtUtc);
        Assert.Null(snapshot.Value.LastSucceededAtUtc);
        Assert.Null(snapshot.Value.LastFailedAtUtc);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
    }

    private static MeterListener CreateJobManagerListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onExecution,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onProcessedItems,
        Action<double, ReadOnlySpan<KeyValuePair<string, object?>>>? onDuration) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, JobManagerMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name is "fooddiary.job.execution.events" or "fooddiary.job.processed_items" or "fooddiary.job.execution.duration") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (!string.Equals(GetTagValue(tags, "fooddiary.job.name"), "fasting.notifications", StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.job.execution.events", StringComparison.Ordinal)) {
                onExecution?.Invoke(value, tags);
            } else if (string.Equals(instrument.Name, "fooddiary.job.processed_items", StringComparison.Ordinal)) {
                onProcessedItems?.Invoke(value, tags);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
            if (!string.Equals(GetTagValue(tags, "fooddiary.job.name"), "fasting.notifications", StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.job.execution.duration", StringComparison.Ordinal)) {
                onDuration?.Invoke(value, tags);
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
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingFastingNotificationScheduler(int result) : IFastingNotificationScheduler {
        public int CallCount { get; private set; }

        public Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingFastingNotificationScheduler : IFastingNotificationScheduler {
        public int CallCount { get; private set; }

        public Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
            CallCount++;
            throw new InvalidOperationException("scheduler failed");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public LogLevel LastLogLevel { get; private set; } = LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            LastLogLevel = logLevel;
        }
    }
}
