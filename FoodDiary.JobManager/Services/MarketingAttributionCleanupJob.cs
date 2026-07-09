using System.Diagnostics;
using FoodDiary.Application.Abstractions.Marketing.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class MarketingAttributionCleanupJob(
    IMarketingAttributionEventWriteRepository repository,
    IOptions<MarketingAttributionCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<MarketingAttributionCleanupJob> logger) {
    private const string JobName = "marketing.attribution_cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        MarketingAttributionCleanupOptions settings = options.Value;
        int totalDeletedCount = 0;

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, deleted: totalDeletedCount);
                return;
            }

            DateTime cutoffUtc = observer.UtcNow.AddDays(-settings.RetentionDays);
            totalDeletedCount = await DeleteEventsAsync(cutoffUtc, settings.BatchSize, cancellationToken).ConfigureAwait(false);

            if (totalDeletedCount > 0) {
                logger.LogInformation(
                    "Deleted {DeletedCount} marketing attribution events older than {CutoffUtc}.",
                    totalDeletedCount,
                    cutoffUtc);
            }

            observer.RecordSuccess(JobName, deleted: totalDeletedCount);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation(
                "Marketing attribution cleanup job was canceled after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Marketing attribution cleanup job failed after deleting {DeletedCount} events.",
                totalDeletedCount);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }

    private async Task<int> DeleteEventsAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken) {
        int totalDeletedCount = 0;
        int deletedCount;

        do {
            cancellationToken.ThrowIfCancellationRequested();
            deletedCount = await repository.DeleteOlderThanAsync(
                cutoffUtc,
                batchSize,
                cancellationToken).ConfigureAwait(false);
            totalDeletedCount += deletedCount;
        } while (deletedCount == batchSize);

        return totalDeletedCount;
    }
}
