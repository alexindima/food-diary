using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Services;

public sealed class JobExecutionStateTracker : IJobExecutionStateTracker, IDisposable {
    private readonly ConcurrentDictionary<string, JobExecutionStateSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly Meter _meter = new(JobManagerTelemetry.MeterName);

    public JobExecutionStateTracker() {
        _meter.CreateObservableGauge(
            "fooddiary.job.last_success_age",
            ObserveLastSuccessAge,
            unit: "s");
        _meter.CreateObservableGauge(
            "fooddiary.job.failure_streak",
            ObserveFailureStreak);
    }

    public void RecordStarted(string jobName, DateTime utcNow) {
        _snapshots.AddOrUpdate(
            jobName,
            static (_, timestamp) => new JobExecutionStateSnapshot(timestamp, LastSucceededAtUtc: null, LastFailedAtUtc: null, 0),
            static (_, current, timestamp) => current with { LastStartedAtUtc = timestamp },
            utcNow);
    }

    public void RecordSuccess(string jobName, DateTime utcNow) {
        _snapshots.AddOrUpdate(
            jobName,
            static (_, timestamp) => new JobExecutionStateSnapshot(timestamp, timestamp, LastFailedAtUtc: null, 0),
            static (_, current, timestamp) => current with {
                LastStartedAtUtc = current.LastStartedAtUtc ?? timestamp,
                LastSucceededAtUtc = timestamp,
                ConsecutiveFailures = 0,
            },
            utcNow);
    }

    public void RecordFailure(string jobName, DateTime utcNow) {
        _snapshots.AddOrUpdate(
            jobName,
            static (_, timestamp) => new JobExecutionStateSnapshot(timestamp, LastSucceededAtUtc: null, timestamp, 1),
            static (_, current, timestamp) => current with {
                LastStartedAtUtc = current.LastStartedAtUtc ?? timestamp,
                LastFailedAtUtc = timestamp,
                ConsecutiveFailures = current.ConsecutiveFailures + 1,
            },
            utcNow);
    }

    public JobExecutionStateSnapshot? GetSnapshot(string jobName) {
        return _snapshots.TryGetValue(jobName, out JobExecutionStateSnapshot snapshot) ? snapshot : null;
    }

    public void Dispose() {
        _meter.Dispose();
    }

    private IEnumerable<Measurement<long>> ObserveLastSuccessAge() {
        DateTime now = TimeProvider.System.GetUtcNow().UtcDateTime;

        foreach (KeyValuePair<string, JobExecutionStateSnapshot> entry in _snapshots) {
            if (entry.Value.LastSucceededAtUtc is not { } lastSucceededAtUtc) {
                continue;
            }

            TimeSpan age = now - lastSucceededAtUtc;
            long ageSeconds = age <= TimeSpan.Zero ? 0L : (long)age.TotalSeconds;
            yield return new Measurement<long>(
                ageSeconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", entry.Key));
        }
    }

    private IEnumerable<Measurement<int>> ObserveFailureStreak() {
        foreach (KeyValuePair<string, JobExecutionStateSnapshot> entry in _snapshots) {
            yield return new Measurement<int>(
                entry.Value.ConsecutiveFailures,
                new KeyValuePair<string, object?>("fooddiary.job.name", entry.Key));
        }
    }
}
