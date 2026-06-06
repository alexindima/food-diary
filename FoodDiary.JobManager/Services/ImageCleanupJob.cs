using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Images.Common;
using Hangfire;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupJob(
    IImageAssetCleanupService cleanupService,
    IOptions<ImageCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<ImageCleanupJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        ImageCleanupOptions settings = options.Value;
        int olderThanHours = settings.OlderThanHours > 0 ? settings.OlderThanHours : 12;
        int batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        DateTime olderThanUtc = dateTimeProvider.UtcNow.AddHours(-olderThanHours);
        int totalDeleted = 0;
        const string jobName = "images.cleanup";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.UtcNow);

        try {
            while (!cancellationToken.IsCancellationRequested) {
                int deleted = await cleanupService.CleanupOrphansAsync(olderThanUtc, batchSize, cancellationToken).ConfigureAwait(false);
                totalDeleted += deleted;

                if (deleted < batchSize) {
                    break;
                }
            }

            if (totalDeleted > 0) {
                logger.LogInformation("Removed {Count} orphaned image assets older than {OlderThan}", totalDeleted, olderThanUtc);
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
            logger.LogError(ex, "Image cleanup job failed after processing {DeletedCount} items so far.", totalDeleted);
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
