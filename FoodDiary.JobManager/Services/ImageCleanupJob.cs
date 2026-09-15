using FoodDiary.Mediator;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupJob(
    ISender sender,
    IOptions<ImageCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<ImageCleanupJob> logger) {
    private const string JobName = "images.cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        ImageCleanupOptions settings = options.Value;
        int olderThanHours = settings.OlderThanHours > 0 ? settings.OlderThanHours : 12;
        int batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        DateTime olderThanUtc = observer.UtcNow.AddHours(-olderThanHours);
        int totalDeleted = 0;

        try {
            cancellationToken.ThrowIfCancellationRequested();
            totalDeleted = await sender.Send(new CleanupOrphanImagesCommand(olderThanUtc, batchSize), cancellationToken).ConfigureAwait(false);

            if (totalDeleted > 0) {
                logger.LogInformation("Removed {Count} orphaned image assets older than {OlderThan}", totalDeleted, olderThanUtc);
            }

            observer.RecordSuccess(JobName, deleted: totalDeleted);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Image cleanup job was canceled after processing {DeletedCount} items.", totalDeleted);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Image cleanup job failed after processing {DeletedCount} items so far.", totalDeleted);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
