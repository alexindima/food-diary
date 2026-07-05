using System.Diagnostics;
using FoodDiary.Application.Abstractions.Email.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class EmailOutboxJob(
    IEmailOutboxProcessor processor,
    IOptions<EmailOutboxOptions> options,
    TimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<EmailOutboxJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        EmailOutboxOptions settings = options.Value;
        const string jobName = "email.outbox";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                RecordSuccess(jobName, processed: 0);
                return;
            }

            int processed = await processor.ProcessDueAsync(settings.BatchSize, cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Processed {ProcessedCount} email outbox messages.", processed);
            }

            RecordSuccess(jobName, processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Email outbox job was canceled.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "canceled"));
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Email outbox job failed.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            executionStateTracker.RecordFailure(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }

    private void RecordSuccess(string jobName, int processed) {
        JobManagerTelemetry.JobExecutionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
            new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
        JobManagerTelemetry.JobProcessedItemsCounter.Add(
            processed,
            new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        executionStateTracker.RecordSuccess(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
    }
}
