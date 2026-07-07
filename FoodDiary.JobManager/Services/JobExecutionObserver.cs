using System.Diagnostics;

namespace FoodDiary.JobManager.Services;

public sealed class JobExecutionObserver(
    TimeProvider timeProvider,
    IJobExecutionStateTracker executionStateTracker) {
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public Stopwatch Start(string jobName) {
        executionStateTracker.RecordStarted(jobName, UtcNow);
        return Stopwatch.StartNew();
    }

    public void RecordSuccess(string jobName, int? processed = null, int? deleted = null) {
        RecordExecution(jobName, "success");

        if (processed.HasValue) {
            JobManagerTelemetry.JobProcessedItemsCounter.Add(
                processed.Value,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }

        if (deleted.HasValue) {
            JobManagerTelemetry.JobDeletedItemsCounter.Add(
                deleted.Value,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }

        executionStateTracker.RecordSuccess(jobName, UtcNow);
    }

    public void RecordCanceled(string jobName) {
        RecordExecution(jobName, "canceled");
    }

    public void RecordFailure(string jobName) {
        RecordExecution(jobName, "failure");
        executionStateTracker.RecordFailure(jobName, UtcNow);
    }

    public static void RecordDuration(string jobName, Stopwatch stopwatch) {
        stopwatch.Stop();
        JobManagerTelemetry.JobExecutionDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
    }

    private static void RecordExecution(string jobName, string outcome) {
        JobManagerTelemetry.JobExecutionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
            new KeyValuePair<string, object?>("fooddiary.job.outcome", outcome));
    }
}