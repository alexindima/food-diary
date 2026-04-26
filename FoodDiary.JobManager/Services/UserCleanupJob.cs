using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    IUserCleanupService cleanupService,
    IOptions<UserCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<UserCleanupJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        var settings = options.Value;
        var retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 30;
        var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        var olderThanUtc = dateTimeProvider.UtcNow.AddDays(-retentionDays);
        var totalDeleted = 0;
        const string jobName = "users.cleanup";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.UtcNow);

        Guid? reassignUserId = null;
        if (!string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out var parsed)) {
            reassignUserId = parsed;
        }

        try {
            while (!cancellationToken.IsCancellationRequested) {
                var deleted = await cleanupService.CleanupDeletedUsersAsync(
                    olderThanUtc,
                    batchSize,
                    reassignUserId,
                    cancellationToken);

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
            executionStateTracker.RecordSuccess(jobName, dateTimeProvider.UtcNow);
        } catch (Exception ex) {
            logger.LogError(ex, "User cleanup job failed after processing {DeletedCount} users so far.", totalDeleted);
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            executionStateTracker.RecordFailure(jobName, dateTimeProvider.UtcNow);
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }
}
