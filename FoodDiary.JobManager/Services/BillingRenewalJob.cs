using System.Diagnostics;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Billing.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class BillingRenewalJob(
    BillingRenewalService billingRenewalService,
    IOptions<BillingRenewalOptions> options,
    TimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<BillingRenewalJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        BillingRenewalOptions settings = options.Value;
        const string jobName = "billing.renewal";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            if (!settings.Enabled) {
                JobManagerTelemetry.JobExecutionCounter.Add(
                    1,
                    new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                    new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
                executionStateTracker.RecordSuccess(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
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

            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
            executionStateTracker.RecordSuccess(jobName, dateTimeProvider.GetUtcNow().UtcDateTime);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Billing renewal job was canceled.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "canceled"));
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Billing renewal job failed.");
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
}
