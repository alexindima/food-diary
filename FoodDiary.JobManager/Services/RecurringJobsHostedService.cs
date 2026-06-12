using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IRecurringJobRegistrationVerifier recurringJobRegistrationVerifier,
    IOptions<ImageCleanupOptions> options,
    IOptions<BillingRenewalOptions> billingRenewalOptions,
    IOptions<NotificationCleanupOptions> notificationCleanupOptions,
    IOptions<UserCleanupOptions> userCleanupOptions) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        ImageCleanupOptions settings = options.Value;
        BillingRenewalOptions billingRenewalSettings = billingRenewalOptions.Value;
        NotificationCleanupOptions notificationSettings = notificationCleanupOptions.Value;
        UserCleanupOptions userSettings = userCleanupOptions.Value;
        string imageCron = string.IsNullOrWhiteSpace(settings.Cron) ? "0 * * * *" : settings.Cron;
        string billingRenewalCron = string.IsNullOrWhiteSpace(billingRenewalSettings.Cron) ? "15 * * * *" : billingRenewalSettings.Cron;
        string notificationCron = string.IsNullOrWhiteSpace(notificationSettings.Cron) ? "0 4 * * *" : notificationSettings.Cron;
        string userCron = string.IsNullOrWhiteSpace(userSettings.Cron) ? "0 3 * * *" : userSettings.Cron;
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageAssetsCleanup,
            Job.FromExpression<ImageCleanupJob>(job => job.Execute(CancellationToken.None)),
            imageCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.BillingRenewal,
            Job.FromExpression<BillingRenewalJob>(job => job.Execute(CancellationToken.None)),
            billingRenewalCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationsCleanup,
            Job.FromExpression<NotificationCleanupJob>(job => job.Execute(CancellationToken.None)),
            notificationCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UsersCleanup,
            Job.FromExpression<UserCleanupJob>(job => job.Execute(CancellationToken.None)),
            userCron);
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
