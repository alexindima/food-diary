using FoodDiary.JobManager.Services;

namespace FoodDiary.JobManager.Tests;

public sealed class JobExecutionStateTrackerTests : IDisposable {
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

    public void Dispose() => _tracker.Dispose();
}
