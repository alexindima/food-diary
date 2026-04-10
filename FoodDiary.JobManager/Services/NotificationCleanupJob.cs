using System.Diagnostics;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Notifications.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class NotificationCleanupJob(
    INotificationCleanupService notificationCleanupService,
    IOptions<NotificationCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<NotificationCleanupJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        var settings = options.Value;
        var totalDeleted = 0;
        const string jobName = "notifications.cleanup";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.UtcNow);

        var policy = new NotificationCleanupPolicy(
            settings.TransientTypes,
            settings.TransientReadRetentionDays,
            settings.TransientUnreadRetentionDays,
            settings.StandardReadRetentionDays,
            settings.StandardUnreadRetentionDays,
            settings.BatchSize);

        try {
            while (!cancellationToken.IsCancellationRequested) {
                var deleted = await notificationCleanupService.CleanupExpiredNotificationsAsync(policy, cancellationToken);
                totalDeleted += deleted;

                if (deleted < settings.BatchSize) {
                    break;
                }
            }

            if (totalDeleted > 0) {
                logger.LogInformation(
                    "Removed {Count} expired notifications using retention policy transient(read/unread)={TransientRead}/{TransientUnread}, standard(read/unread)={StandardRead}/{StandardUnread}.",
                    totalDeleted,
                    settings.TransientReadRetentionDays,
                    settings.TransientUnreadRetentionDays,
                    settings.StandardReadRetentionDays,
                    settings.StandardUnreadRetentionDays);
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
            logger.LogError(ex, "Notification cleanup job failed after deleting {DeletedCount} notifications so far.", totalDeleted);
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
