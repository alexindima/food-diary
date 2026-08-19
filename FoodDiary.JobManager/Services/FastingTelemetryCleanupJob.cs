using System.Diagnostics;
using FoodDiary.Application.Fasting.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class FastingTelemetryCleanupJob(
    IFastingTelemetryCleanupService cleanupService,
    IOptions<FastingTelemetryCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<FastingTelemetryCleanupJob> logger) {
    private const string JobName = "fasting.telemetry_cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        FastingTelemetryCleanupOptions settings = options.Value;
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
                    "Deleted {DeletedCount} fasting telemetry events older than {CutoffUtc}.",
                    totalDeletedCount,
                    cutoffUtc);
            }

            observer.RecordSuccess(JobName, deleted: totalDeletedCount);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation(
                "Fasting telemetry cleanup job was canceled after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Fasting telemetry cleanup job failed after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
