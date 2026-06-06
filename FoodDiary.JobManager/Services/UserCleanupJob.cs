using Hangfire;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    IUserCleanupService cleanupService,
    IOptions<UserCleanupOptions> options,
    TimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<UserCleanupJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        UserCleanupOptions settings = options.Value;
        int retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 30;
        int batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        DateTime olderThanUtc = dateTimeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);
        int totalDeleted = 0;
        const string jobName = "users.cleanup";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);

        Guid? reassignUserId = null;
        if (!string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out Guid parsed)) {
            reassignUserId = parsed;
        }

        try {
            while (!cancellationToken.IsCancellationRequested) {
                int deleted = await cleanupService.CleanupDeletedUsersAsync(
                    olderThanUtc,
                    batchSize,
                    reassignUserId,
                    cancellationToken).ConfigureAwait(false);

                totalDeleted += deleted;

                if (deleted < batchSize) {
                    break;
                }
            }

            if (totalDeleted > 0) {
                logger.LogInformation("Removed {Count} users deleted before {OlderThan}", totalDeleted, olderThanUtc);
            }

            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
            JobManagerTelemetry.JobDeletedItemsCounter.Add(
                totalDeleted,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
            executionStateTracker.RecordSuccess(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
        } catch (Exception ex) {
            logger.LogError(ex, "User cleanup job failed after processing {DeletedCount} users so far.", totalDeleted);
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
}
