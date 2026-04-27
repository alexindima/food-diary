using System.Diagnostics;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class BillingRenewalJob(
    BillingRenewalService billingRenewalService,
    IOptions<BillingRenewalOptions> options,
    IDateTimeProvider dateTimeProvider,
    IJobExecutionStateTracker executionStateTracker,
    ILogger<BillingRenewalJob> logger) {
    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        var settings = options.Value;
        const string jobName = "billing.renewal";
        executionStateTracker.RecordStarted(jobName, dateTimeProvider.UtcNow);

        try {
            if (!settings.Enabled) {
                executionStateTracker.RecordSuccess(jobName, dateTimeProvider.UtcNow);
                return;
            }

            var result = await billingRenewalService.RenewDueSubscriptionsAsync(
                settings.Provider,
                settings.BatchSize,
                cancellationToken);

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
            executionStateTracker.RecordSuccess(jobName, dateTimeProvider.UtcNow);
        } catch (Exception ex) {
            logger.LogError(ex, "Billing renewal job failed.");
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            executionStateTracker.RecordFailure(jobName, dateTimeProvider.UtcNow);
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }
}
