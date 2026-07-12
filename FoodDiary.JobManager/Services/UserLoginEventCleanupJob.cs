using System.Diagnostics;
using FoodDiary.Application.Authentication.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserLoginEventCleanupJob(
    IAuthenticationLoginEventCleanupService cleanupService,
    IOptions<UserLoginEventCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<UserLoginEventCleanupJob> logger) {
    private const string JobName = "users.login_events_cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        UserLoginEventCleanupOptions settings = options.Value;
        int totalDeletedCount = 0;

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, deleted: totalDeletedCount);
                return;
            }

            DateTime cutoffUtc = observer.UtcNow.AddDays(-settings.RetentionDays);
            totalDeletedCount = await cleanupService
                .CleanupAsync(cutoffUtc, settings.BatchSize, cancellationToken)
                .ConfigureAwait(false);

            if (totalDeletedCount > 0) {
                logger.LogInformation(
                    "Deleted {DeletedCount} user login events older than {CutoffUtc}.",
                    totalDeletedCount,
                    cutoffUtc);
            }

            observer.RecordSuccess(JobName, deleted: totalDeletedCount);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation(
                "User login event cleanup job was canceled after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "User login event cleanup job failed after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }

}
