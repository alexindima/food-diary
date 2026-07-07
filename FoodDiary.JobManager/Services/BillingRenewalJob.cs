using System.Diagnostics;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Billing.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class BillingRenewalJob(
    BillingRenewalService billingRenewalService,
    IOptions<BillingRenewalOptions> options,
    JobExecutionObserver observer,
    ILogger<BillingRenewalJob> logger) {
    private const string JobName = "billing.renewal";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        BillingRenewalOptions settings = options.Value;

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            BillingRenewalRunResult result = await billingRenewalService.RenewDueSubscriptionsAsync(
                settings.Provider,
                settings.BatchSize,
                cancellationToken).ConfigureAwait(false);

            if (result.Processed > 0) {
                logger.LogInformation(
                    "Processed {Processed} billing renewals for provider {Provider}: renewed={Renewed}, failed={Failed}.",
                    result.Processed,
                    settings.Provider,
                    result.Renewed,
                    result.Failed);
            }

            observer.RecordSuccess(JobName, processed: result.Processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Billing renewal job was canceled.");
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Billing renewal job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
