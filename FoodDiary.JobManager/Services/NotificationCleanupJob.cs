using System.Diagnostics;
using FoodDiary.Application.Abstractions.Notifications.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class NotificationCleanupJob(
    INotificationCleanupService notificationCleanupService,
    IOptions<NotificationCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<NotificationCleanupJob> logger) {
    private const string JobName = "notifications.cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        NotificationCleanupOptions settings = options.Value;
        NotificationCleanupPolicy policy = CreatePolicy(settings);
        int totalDeleted = 0;

        try {
            totalDeleted = await DeleteNotificationsAsync(policy, settings.BatchSize, cancellationToken).ConfigureAwait(false);

            if (totalDeleted > 0) {
                logger.LogInformation(
                    "Removed {Count} expired notifications using retention policy transient(read/unread)={TransientRead}/{TransientUnread}, standard(read/unread)={StandardRead}/{StandardUnread}.",
                    totalDeleted,
                    settings.TransientReadRetentionDays,
                    settings.TransientUnreadRetentionDays,
                    settings.StandardReadRetentionDays,
                    settings.StandardUnreadRetentionDays);
            }

            observer.RecordSuccess(JobName, deleted: totalDeleted);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Notification cleanup job was canceled after deleting {DeletedCount} notifications.", totalDeleted);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Notification cleanup job failed after deleting {DeletedCount} notifications so far.", totalDeleted);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }

    private async Task<int> DeleteNotificationsAsync(
        NotificationCleanupPolicy policy,
        int batchSize,
        CancellationToken cancellationToken) {
        int totalDeleted = 0;

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            int deleted = await notificationCleanupService.CleanupExpiredNotificationsAsync(policy, cancellationToken).ConfigureAwait(false);
            totalDeleted += deleted;

            if (deleted < batchSize) {
                return totalDeleted;
            }
        }
    }

    private static NotificationCleanupPolicy CreatePolicy(NotificationCleanupOptions settings) =>
        new(
            settings.TransientTypes,
            settings.TransientReadRetentionDays,
            settings.TransientUnreadRetentionDays,
            settings.StandardReadRetentionDays,
            settings.StandardUnreadRetentionDays,
            settings.BatchSize);
}