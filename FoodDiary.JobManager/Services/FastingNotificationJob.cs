using System.Diagnostics;
using FoodDiary.Application.Fasting.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class FastingNotificationJob(
    IFastingNotificationScheduler scheduler,
    IOptions<FastingNotificationOptions> options,
    JobExecutionObserver observer,
    ILogger<FastingNotificationJob> logger) {
    private const string JobName = "fasting.notifications";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        FastingNotificationOptions settings = options.Value;
        Stopwatch stopwatch = observer.Start(JobName);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            int processed = await scheduler.ProcessDueNotificationsAsync(cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Created {NotificationCount} fasting notifications.", processed);
            }

            observer.RecordSuccess(JobName, processed: processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Fasting notification job was canceled.");
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Fasting notification job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
