using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IRecurringJobRegistrationVerifier recurringJobRegistrationVerifier,
    IOptions<ImageCleanupOptions> options,
    IOptions<BillingRenewalOptions> billingRenewalOptions,
    IOptions<NotificationCleanupOptions> notificationCleanupOptions,
    IOptions<UserCleanupOptions> userCleanupOptions,
    ImageCleanupJob imageCleanupJob,
    BillingRenewalJob billingRenewalJob,
    NotificationCleanupJob notificationCleanupJob,
    UserCleanupJob userCleanupJob) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        var settings = options.Value;
        var billingRenewalSettings = billingRenewalOptions.Value;
        var notificationSettings = notificationCleanupOptions.Value;
        var userSettings = userCleanupOptions.Value;
        var imageCron = string.IsNullOrWhiteSpace(settings.Cron) ? "0 * * * *" : settings.Cron;
        var billingRenewalCron = string.IsNullOrWhiteSpace(billingRenewalSettings.Cron) ? "15 * * * *" : billingRenewalSettings.Cron;
        var notificationCron = string.IsNullOrWhiteSpace(notificationSettings.Cron) ? "0 4 * * *" : notificationSettings.Cron;
        var userCron = string.IsNullOrWhiteSpace(userSettings.Cron) ? "0 3 * * *" : userSettings.Cron;
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageAssetsCleanup,
            () => imageCleanupJob.Execute(CancellationToken.None),
            imageCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.BillingRenewal,
            () => billingRenewalJob.Execute(CancellationToken.None),
            billingRenewalCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationsCleanup,
            () => notificationCleanupJob.Execute(CancellationToken.None),
            notificationCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UsersCleanup,
            () => userCleanupJob.Execute(CancellationToken.None),
            userCron);
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
