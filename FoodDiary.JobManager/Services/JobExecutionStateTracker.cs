using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Services;

public sealed class JobExecutionStateTracker : IJobExecutionStateTracker, IDisposable {
    private readonly ConcurrentDictionary<string, JobExecutionStateSnapshot> snapshots = new(StringComparer.Ordinal);
    private readonly Meter meter = new(JobManagerTelemetry.MeterName);

    public JobExecutionStateTracker() {
        meter.CreateObservableGauge(
            "fooddiary.job.last_success_age",
            ObserveLastSuccessAge,
            unit: "s");
        meter.CreateObservableGauge(
            "fooddiary.job.failure_streak",
            ObserveFailureStreak);
    }

    public void RecordStarted(string jobName, DateTime utcNow) {
        snapshots.AddOrUpdate(
            jobName,
            _ => new JobExecutionStateSnapshot(utcNow, LastSucceededAtUtc: null, LastFailedAtUtc: null, 0),
            (_, current) => current with { LastStartedAtUtc = utcNow });
    }

    public void RecordSuccess(string jobName, DateTime utcNow) {
        snapshots.AddOrUpdate(
            jobName,
            _ => new JobExecutionStateSnapshot(utcNow, utcNow, LastFailedAtUtc: null, 0),
            (_, current) => current with {
                LastStartedAtUtc = current.LastStartedAtUtc ?? utcNow,
                LastSucceededAtUtc = utcNow,
                ConsecutiveFailures = 0,
            });
    }

    public void RecordFailure(string jobName, DateTime utcNow) {
        snapshots.AddOrUpdate(
            jobName,
            _ => new JobExecutionStateSnapshot(utcNow, LastSucceededAtUtc: null, utcNow, 1),
            (_, current) => current with {
                LastStartedAtUtc = current.LastStartedAtUtc ?? utcNow,
                LastFailedAtUtc = utcNow,
                ConsecutiveFailures = current.ConsecutiveFailures + 1,
            });
    }

    public JobExecutionStateSnapshot? GetSnapshot(string jobName) {
        return snapshots.TryGetValue(jobName, out JobExecutionStateSnapshot snapshot) ? snapshot : null;
    }

    public void Dispose() {
        meter.Dispose();
    }

    private IEnumerable<Measurement<long>> ObserveLastSuccessAge() {
        DateTime now = TimeProvider.System.GetUtcNow().UtcDateTime;

        foreach (KeyValuePair<string, JobExecutionStateSnapshot> entry in snapshots) {
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
        foreach (KeyValuePair<string, JobExecutionStateSnapshot> entry in snapshots) {
            yield return new Measurement<int>(
                entry.Value.ConsecutiveFailures,
                new KeyValuePair<string, object?>("fooddiary.job.name", entry.Key));
        }
    }
}
