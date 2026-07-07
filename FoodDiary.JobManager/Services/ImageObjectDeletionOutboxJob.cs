using System.Diagnostics;
using FoodDiary.Application.Abstractions.Images.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class ImageObjectDeletionOutboxJob(
    IImageObjectDeletionOutboxProcessor processor,
    IOptions<ImageObjectDeletionOutboxOptions> options,
    JobExecutionObserver observer,
    ILogger<ImageObjectDeletionOutboxJob> logger) {
    private const string JobName = "images.object_deletion_outbox";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        ImageObjectDeletionOutboxOptions settings = options.Value;
        Stopwatch stopwatch = observer.Start(JobName);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            int processed = await processor.ProcessDueAsync(settings.BatchSize, cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Processed {ProcessedCount} image object deletion outbox messages.", processed);
            }

            observer.RecordSuccess(JobName, processed: processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Image object deletion outbox job was canceled.");
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Image object deletion outbox job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
