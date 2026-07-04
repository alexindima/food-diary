using System.Diagnostics;
using FoodDiary.Application.Fasting.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class FastingNotificationJob(
    IFastingNotificationScheduler scheduler,
    IOptions<FastingNotificationOptions> options,
    TimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<FastingNotificationJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        FastingNotificationOptions settings = options.Value;
        const string jobName = "fasting.notifications";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                RecordSuccess(jobName, processed: 0);
                return;
            }

            int processed = await scheduler.ProcessDueNotificationsAsync(cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Created {NotificationCount} fasting notifications.", processed);
            }

            RecordSuccess(jobName, processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Fasting notification job was canceled.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "canceled"));
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Fasting notification job failed.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            executionStateTracker.RecordFailure(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }

    private void RecordSuccess(string jobName, int processed) {
        JobManagerTelemetry.JobExecutionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
            new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
        JobManagerTelemetry.JobProcessedItemsCounter.Add(
            processed,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        executionStateTracker.RecordSuccess(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
    }
}
