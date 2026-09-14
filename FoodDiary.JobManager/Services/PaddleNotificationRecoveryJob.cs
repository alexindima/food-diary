using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.ReplayFailedPaddleNotifications;
using FoodDiary.Modules.Billing.Contracts.Models;
using Hangfire;

namespace FoodDiary.JobManager.Services;

public sealed class PaddleNotificationRecoveryJob(
    ISender sender,
    JobExecutionObserver observer,
    ILogger<PaddleNotificationRecoveryJob> logger) {
    private const string JobName = "billing.paddle-notification-recovery";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            PaddleNotificationRecoveryResult result = await sender
                .Send(new ReplayFailedPaddleNotificationsCommand(), cancellationToken)
                .ConfigureAwait(false);
            if (result.Replayed > 0) {
                logger.LogWarning(
                    "Replayed failed Paddle notifications: inspected={Inspected}, replayed={Replayed}.",
                    result.Inspected,
                    result.Replayed);
            }

            if (result.WasLimited) {
                logger.LogWarning(
                    "Paddle notification recovery stopped at its per-run limit: inspected={Inspected}, replayed={Replayed}.",
                    result.Inspected,
                    result.Replayed);
            }

            observer.RecordSuccess(JobName, result.Replayed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Paddle notification recovery job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
