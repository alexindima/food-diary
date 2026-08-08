using System.Diagnostics;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using Hangfire;

namespace FoodDiary.JobManager.Services;

public sealed class BillingWebhookInboxJob(
    IBillingWebhookInboxService billingWebhookInboxService,
    JobExecutionObserver observer,
    ILogger<BillingWebhookInboxJob> logger) {
    private const string JobName = "billing.webhook-inbox";
    private const int BatchSize = 100;

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            BillingWebhookInboxRunResult result = await billingWebhookInboxService
                .ProcessPendingAsync(BatchSize, cancellationToken)
                .ConfigureAwait(false);

            if (result.Processed > 0 || result.Failed > 0) {
                logger.LogInformation(
                    "Processed billing webhook inbox batch: processed={Processed}, failed={Failed}.",
                    result.Processed,
                    result.Failed);
            }

            observer.RecordSuccess(JobName, result.Processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Billing webhook inbox job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
