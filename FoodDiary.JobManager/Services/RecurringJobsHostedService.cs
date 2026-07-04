using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IRecurringJobRegistrationVerifier recurringJobRegistrationVerifier,
    IOptions<ImageCleanupOptions> options,
    IOptions<BillingRenewalOptions> billingRenewalOptions,
    IOptions<FastingNotificationOptions> fastingNotificationOptions,
    IOptions<ImageObjectDeletionOutboxOptions> imageObjectDeletionOutboxOptions,
    IOptions<NotificationWebPushOutboxOptions> notificationWebPushOutboxOptions,
    IOptions<NotificationCleanupOptions> notificationCleanupOptions,
    IOptions<UserLoginEventCleanupOptions> userLoginEventCleanupOptions,
    IOptions<UserCleanupOptions> userCleanupOptions) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        ImageCleanupOptions settings = options.Value;
        BillingRenewalOptions billingRenewalSettings = billingRenewalOptions.Value;
        FastingNotificationOptions fastingNotificationSettings = fastingNotificationOptions.Value;
        ImageObjectDeletionOutboxOptions imageOutboxSettings = imageObjectDeletionOutboxOptions.Value;
        NotificationWebPushOutboxOptions notificationOutboxSettings = notificationWebPushOutboxOptions.Value;
        NotificationCleanupOptions notificationSettings = notificationCleanupOptions.Value;
        UserLoginEventCleanupOptions userLoginEventSettings = userLoginEventCleanupOptions.Value;
        UserCleanupOptions userSettings = userCleanupOptions.Value;
        string imageCron = string.IsNullOrWhiteSpace(settings.Cron) ? "0 * * * *" : settings.Cron;
        string billingRenewalCron = string.IsNullOrWhiteSpace(billingRenewalSettings.Cron) ? "15 * * * *" : billingRenewalSettings.Cron;
        string fastingNotificationCron = string.IsNullOrWhiteSpace(fastingNotificationSettings.Cron) ? "* * * * *" : fastingNotificationSettings.Cron;
        string imageOutboxCron = string.IsNullOrWhiteSpace(imageOutboxSettings.Cron) ? "* * * * *" : imageOutboxSettings.Cron;
        string notificationOutboxCron = string.IsNullOrWhiteSpace(notificationOutboxSettings.Cron) ? "* * * * *" : notificationOutboxSettings.Cron;
        string notificationCron = string.IsNullOrWhiteSpace(notificationSettings.Cron) ? "0 4 * * *" : notificationSettings.Cron;
        string userLoginEventCron = string.IsNullOrWhiteSpace(userLoginEventSettings.Cron) ? "0 3 * * *" : userLoginEventSettings.Cron;
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
            RecurringJobIds.FastingNotifications,
            Job.FromExpression<FastingNotificationJob>(job => job.Execute(CancellationToken.None)),
            fastingNotificationCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageObjectDeletionOutbox,
            Job.FromExpression<ImageObjectDeletionOutboxJob>(job => job.Execute(CancellationToken.None)),
            imageOutboxCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationWebPushOutbox,
            Job.FromExpression<NotificationWebPushOutboxJob>(job => job.Execute(CancellationToken.None)),
            notificationOutboxCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationsCleanup,
            Job.FromExpression<NotificationCleanupJob>(job => job.Execute(CancellationToken.None)),
            notificationCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UsersCleanup,
            Job.FromExpression<UserCleanupJob>(job => job.Execute(CancellationToken.None)),
            userCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UserLoginEventsCleanup,
            Job.FromExpression<UserLoginEventCleanupJob>(job => job.Execute(CancellationToken.None)),
            userLoginEventCron);
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
