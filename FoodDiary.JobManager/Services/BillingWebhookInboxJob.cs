using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.ProcessBillingWebhookInbox;
using FoodDiary.Modules.Billing.Contracts.Models;
using Hangfire;

namespace FoodDiary.JobManager.Services;

public sealed class BillingWebhookInboxJob(
    ISender sender,
    JobExecutionObserver observer,
    ILogger<BillingWebhookInboxJob> logger) {
    private const string JobName = "billing.webhook-inbox";
    private const int BatchSize = 100;

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            BillingWebhookInboxRunResult result = await sender
                .Send(new ProcessBillingWebhookInboxCommand(BatchSize), cancellationToken)
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
