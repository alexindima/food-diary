using System.Diagnostics;
using FoodDiary.Application.Abstractions.Achievements.Common;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class AchievementEvaluationOutboxJob(
    IAchievementEvaluationOutboxProcessor processor,
    IOptions<AchievementEvaluationOutboxOptions> options,
    JobExecutionObserver observer,
    ILogger<AchievementEvaluationOutboxJob> logger) {
    private const string JobName = "achievements.evaluation_outbox";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        AchievementEvaluationOutboxOptions settings = options.Value;
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            cancellationToken.ThrowIfCancellationRequested();
            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            int processed = await processor.ProcessDueAsync(settings.BatchSize, cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Processed {ProcessedCount} achievement evaluation messages.", processed);
            }
            observer.RecordSuccess(JobName, processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception exception) {
            logger.LogError(exception, "Achievement evaluation outbox job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
