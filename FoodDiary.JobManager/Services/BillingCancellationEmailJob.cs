using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using Hangfire;

namespace FoodDiary.JobManager.Services;

public sealed class BillingCancellationEmailJob(ISender sender, JobExecutionObserver observer,
    ILogger<BillingCancellationEmailJob> logger) {
    private const string JobName = "billing.cancellation-email";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            BillingCancellationEmailRunResult result = await sender.Send(new SendCancellationConfirmationsCommand(100), cancellationToken);
            if (result.Queued > 0 || result.Failed > 0) {
                logger.LogInformation("Cancellation confirmations: queued={Queued}, skipped={Skipped}, failed={Failed}.",
                    result.Queued, result.Skipped, result.Failed);
            }
            observer.RecordSuccess(JobName, result.Queued + result.Skipped);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception exception) {
            logger.LogError(exception, "Cancellation confirmation job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
