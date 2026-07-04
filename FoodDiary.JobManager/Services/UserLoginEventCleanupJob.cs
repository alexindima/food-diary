using System.Diagnostics;
using FoodDiary.Application.Abstractions.Authentication.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserLoginEventCleanupJob(
    IUserLoginEventRepository repository,
    IOptions<UserLoginEventCleanupOptions> options,
    TimeProvider timeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<UserLoginEventCleanupJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        UserLoginEventCleanupOptions settings = options.Value;
        const string jobName = "users.login_events_cleanup";
        executionStateTracker.RecordStarted(jobName, timeProvider.GetUtcNow().UtcDateTime);
        int totalDeletedCount = 0;

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                RecordSuccess(jobName, totalDeletedCount);
                return;
            }

            DateTime cutoffUtc = timeProvider.GetUtcNow().UtcDateTime.AddDays(-settings.RetentionDays);
            int deletedCount;
            do {
                cancellationToken.ThrowIfCancellationRequested();
                deletedCount = await repository.DeleteOlderThanAsync(
                    cutoffUtc,
                    settings.BatchSize,
                    cancellationToken).ConfigureAwait(false);
                totalDeletedCount += deletedCount;
            } while (deletedCount == settings.BatchSize);

            if (totalDeletedCount > 0) {
                logger.LogInformation(
                    "Deleted {DeletedCount} user login events older than {CutoffUtc}.",
                    totalDeletedCount,
                    cutoffUtc);
            }

            RecordSuccess(jobName, totalDeletedCount);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation(
                "User login event cleanup job was canceled after deleting {DeletedCount} events.",
                totalDeletedCount);
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "canceled"));
            throw;
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "User login event cleanup job failed after deleting {DeletedCount} events.",
                totalDeletedCount);
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            executionStateTracker.RecordFailure(jobName, timeProvider.GetUtcNow().UtcDateTime);
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }

    private void RecordSuccess(string jobName, int totalDeletedCount) {
        JobManagerTelemetry.JobExecutionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
            new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
        JobManagerTelemetry.JobDeletedItemsCounter.Add(
            totalDeletedCount,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        executionStateTracker.RecordSuccess(jobName, timeProvider.GetUtcNow().UtcDateTime);
    }
}
