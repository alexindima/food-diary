using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.SendTelegramCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using Hangfire;

namespace FoodDiary.JobManager.Services;

public sealed class BillingCancellationTelegramJob(ISender sender, JobExecutionObserver observer,
    ILogger<BillingCancellationTelegramJob> logger) {
    private const string JobName = "billing.cancellation-telegram";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            BillingCancellationTelegramRunResult result = await sender.Send(new SendTelegramCancellationConfirmationsCommand(100), cancellationToken);
            if (result.Sent > 0 || result.Failed > 0) {
                logger.LogInformation("Telegram cancellation confirmations: sent={Sent}, skipped={Skipped}, failed={Failed}.",
                    result.Sent, result.Skipped, result.Failed);
            }
            observer.RecordSuccess(JobName, result.Sent + result.Skipped);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception exception) {
            logger.LogError(exception, "Telegram cancellation confirmation job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
