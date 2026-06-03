using FoodDiary.JobManager.Services;
using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Tests;

public sealed class JobExecutionStateTrackerTests : IDisposable {
    private const string JobManagerMeterName = "FoodDiary.JobManager";

    private readonly JobExecutionStateTracker _tracker = new();

    [Fact]
    public void GetSnapshot_WhenNoRecords_ReturnsNull() {
        Assert.Null(_tracker.GetSnapshot("unknown-job"));
    }

    [Fact]
    public void RecordStarted_CreatesSnapshot() {
        var now = DateTime.UtcNow;
        _tracker.RecordStarted("test-job", now);

        var snapshot = _tracker.GetSnapshot("test-job");

        Assert.NotNull(snapshot);
        Assert.Equal(now, snapshot.Value.LastStartedAtUtc);
        Assert.Null(snapshot.Value.LastSucceededAtUtc);
        Assert.Null(snapshot.Value.LastFailedAtUtc);
        Assert.Equal(0, snapshot.Value.ConsecutiveFailures);
    }

    [Fact]
    public void RecordStarted_UpdatesExistingSnapshot() {
        var now = DateTime.UtcNow;
        _tracker.RecordSuccess("test-job", now);

        _tracker.RecordStarted("test-job", now.AddMinutes(1));

        var snapshot = _tracker.GetSnapshot("test-job");
        Assert.Equal(now.AddMinutes(1), snapshot!.Value.LastStartedAtUtc);
        Assert.Equal(now, snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public void RecordSuccess_ResetsConsecutiveFailures() {
        var now = DateTime.UtcNow;
        _tracker.RecordFailure("test-job", now);
        _tracker.RecordFailure("test-job", now.AddSeconds(1));
        _tracker.RecordSuccess("test-job", now.AddSeconds(2));

        var snapshot = _tracker.GetSnapshot("test-job");

        Assert.Equal(0, snapshot!.Value.ConsecutiveFailures);
        Assert.Equal(now.AddSeconds(2), snapshot.Value.LastSucceededAtUtc);
    }

    [Fact]
    public void RecordFailure_IncrementsConsecutiveFailures() {
        var now = DateTime.UtcNow;
        _tracker.RecordFailure("test-job", now);
        _tracker.RecordFailure("test-job", now.AddSeconds(1));
        _tracker.RecordFailure("test-job", now.AddSeconds(2));

        var snapshot = _tracker.GetSnapshot("test-job");

        Assert.Equal(3, snapshot!.Value.ConsecutiveFailures);
        Assert.Equal(now.AddSeconds(2), snapshot.Value.LastFailedAtUtc);
    }

    [Fact]
    public void MultipleJobs_TrackIndependently() {
        var now = DateTime.UtcNow;
        _tracker.RecordSuccess("job-a", now);
        _tracker.RecordFailure("job-b", now);

        var a = _tracker.GetSnapshot("job-a");
        var b = _tracker.GetSnapshot("job-b");

        Assert.Equal(0, a!.Value.ConsecutiveFailures);
        Assert.Equal(1, b!.Value.ConsecutiveFailures);
    }

    [Fact]
    public void ObservableGauges_ReportSuccessAgeAndFailureStreak() {
        var successAges = new List<long>();
        var failureStreaks = new Dictionary<string, int>(StringComparer.Ordinal);
        var now = DateTime.UtcNow;
        _tracker.RecordSuccess("job-a", now.AddSeconds(-10));
        _tracker.RecordStarted("job-b", now);
        _tracker.RecordFailure("job-b", now.AddSeconds(1));

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, JobManagerMeterName, StringComparison.Ordinal) &&
                instrument.Name is "fooddiary.job.last_success_age" or "fooddiary.job.failure_streak") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => {
            if (string.Equals(instrument.Name, "fooddiary.job.last_success_age", StringComparison.Ordinal)) {
                successAges.Add(value);
            }
        });
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => {
            if (!string.Equals(instrument.Name, "fooddiary.job.failure_streak", StringComparison.Ordinal)) {
                return;
            }

            var jobName = GetTagValue(tags, "fooddiary.job.name");
            if (jobName is not null) {
                failureStreaks[jobName] = value;
            }
        });

        listener.Start();
        listener.RecordObservableInstruments();

        Assert.Contains(successAges, age => age >= 0);
        Assert.Equal(0, failureStreaks["job-a"]);
        Assert.Equal(1, failureStreaks["job-b"]);
    }

    public void Dispose() => _tracker.Dispose();

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
