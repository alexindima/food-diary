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
    IOptions<EmailOutboxOptions> emailOutboxOptions,
    IOptions<NotificationWebPushOutboxOptions> notificationWebPushOutboxOptions,
    IOptions<NotificationCleanupOptions> notificationCleanupOptions,
    IOptions<UserLoginEventCleanupOptions> userLoginEventCleanupOptions,
    IOptions<UserCleanupOptions> userCleanupOptions) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        ImageCleanupOptions settings = options.Value;
        BillingRenewalOptions billingRenewalSettings = billingRenewalOptions.Value;
        FastingNotificationOptions fastingNotificationSettings = fastingNotificationOptions.Value;
        ImageObjectDeletionOutboxOptions imageOutboxSettings = imageObjectDeletionOutboxOptions.Value;
        EmailOutboxOptions emailOutboxSettings = emailOutboxOptions.Value;
        NotificationWebPushOutboxOptions notificationOutboxSettings = notificationWebPushOutboxOptions.Value;
        NotificationCleanupOptions notificationSettings = notificationCleanupOptions.Value;
        UserLoginEventCleanupOptions userLoginEventSettings = userLoginEventCleanupOptions.Value;
        UserCleanupOptions userSettings = userCleanupOptions.Value;
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageAssetsCleanup,
            Job.FromExpression<ImageCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(settings.Cron, "0 * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.BillingRenewal,
            Job.FromExpression<BillingRenewalJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(billingRenewalSettings.Cron, "15 * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.FastingNotifications,
            Job.FromExpression<FastingNotificationJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(fastingNotificationSettings.Cron, "* * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageObjectDeletionOutbox,
            Job.FromExpression<ImageObjectDeletionOutboxJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(imageOutboxSettings.Cron, "* * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.EmailOutbox,
            Job.FromExpression<EmailOutboxJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(emailOutboxSettings.Cron, "* * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationWebPushOutbox,
            Job.FromExpression<NotificationWebPushOutboxJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(notificationOutboxSettings.Cron, "* * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.NotificationsCleanup,
            Job.FromExpression<NotificationCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(notificationSettings.Cron, "0 4 * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UsersCleanup,
            Job.FromExpression<UserCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(userSettings.Cron, "0 3 * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UserLoginEventsCleanup,
            Job.FromExpression<UserLoginEventCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(userLoginEventSettings.Cron, "0 3 * * *"));
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string ResolveCron(string? configuredCron, string fallbackCron) =>
        string.IsNullOrWhiteSpace(configuredCron) ? fallbackCron : configuredCron;
}
