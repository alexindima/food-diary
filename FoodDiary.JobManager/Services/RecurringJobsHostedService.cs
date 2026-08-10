using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
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
    IOptions<MarketingAttributionCleanupOptions> marketingAttributionCleanupOptions,
    IOptions<UserCleanupOptions> userCleanupOptions,
    IOptions<ClientTaskReminderOptions> clientTaskReminderOptions,
    IOptions<WeeklyGoalReminderOptions> weeklyGoalReminderOptions,
    ILogger<RecurringJobsHostedService> logger,
    IOptions<AchievementEvaluationOutboxOptions>? achievementEvaluationOutboxOptions = null) : IHostedService {
    private static readonly TimeSpan RegistrationRetryDelay = TimeSpan.FromSeconds(1);

    public async Task StartAsync(CancellationToken cancellationToken) {
        int retryCount = 0;

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();

            try {
                RegisterJobs();

                if (retryCount > 0) {
                    logger.LogInformation(
                        "Recurring job registration recovered after {RetryCount} distributed lock retries.",
                        retryCount);
                }

                return;
            } catch (DistributedLockTimeoutException) {
                cancellationToken.ThrowIfCancellationRequested();
                retryCount++;

                if (retryCount == 1) {
                    logger.LogWarning(
                        "Recurring job registration could not acquire a distributed lock. " +
                        "The JobManager will retry without restarting the host.");
                }

                await Task.Delay(RegistrationRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RegisterJobs() {
        ImageCleanupOptions settings = options.Value;
        BillingRenewalOptions billingRenewalSettings = billingRenewalOptions.Value;
        FastingNotificationOptions fastingNotificationSettings = fastingNotificationOptions.Value;
        ImageObjectDeletionOutboxOptions imageOutboxSettings = imageObjectDeletionOutboxOptions.Value;
        EmailOutboxOptions emailOutboxSettings = emailOutboxOptions.Value;
        NotificationWebPushOutboxOptions notificationOutboxSettings = notificationWebPushOutboxOptions.Value;
        AchievementEvaluationOutboxOptions achievementOutboxSettings = achievementEvaluationOutboxOptions?.Value ?? new();
        NotificationCleanupOptions notificationSettings = notificationCleanupOptions.Value;
        UserLoginEventCleanupOptions userLoginEventSettings = userLoginEventCleanupOptions.Value;
        MarketingAttributionCleanupOptions marketingAttributionSettings = marketingAttributionCleanupOptions.Value;
        UserCleanupOptions userSettings = userCleanupOptions.Value;
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageAssetsCleanup,
            Job.FromExpression<ImageCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(settings.Cron, "0 * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.BillingRenewal,
            Job.FromExpression<BillingRenewalJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(billingRenewalSettings.Cron, "15 * * * *"));
        RegisterBillingJobs();
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
        RegisterAchievementOutboxJob(achievementOutboxSettings);
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
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.MarketingAttributionCleanup,
            Job.FromExpression<MarketingAttributionCleanupJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(marketingAttributionSettings.Cron, "30 3 * * *"));
        RegisterReminderJobs();
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);
    }

    private void RegisterReminderJobs() {
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ClientTaskReminders,
            Job.FromExpression<ClientTaskReminderJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(clientTaskReminderOptions.Value.Cron, "0 * * * *"));
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.WeeklyGoalReminders,
            Job.FromExpression<WeeklyGoalReminderJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(weeklyGoalReminderOptions.Value.Cron, "*/15 * * * *"));
    }

    private void RegisterBillingJobs() {
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.BillingWebhookInbox,
            Job.FromExpression<BillingWebhookInboxJob>(job => job.Execute(CancellationToken.None)),
            "* * * * *");
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.PaddleNotificationRecovery,
            Job.FromExpression<PaddleNotificationRecoveryJob>(job => job.Execute(CancellationToken.None)),
            "17 * * * *");
    }

    private void RegisterAchievementOutboxJob(AchievementEvaluationOutboxOptions settings) =>
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.AchievementEvaluationOutbox,
            Job.FromExpression<AchievementEvaluationOutboxJob>(job => job.Execute(CancellationToken.None)),
            ResolveCron(settings.Cron, "* * * * *"));

    private static string ResolveCron(string? configuredCron, string fallbackCron) =>
        string.IsNullOrWhiteSpace(configuredCron) ? fallbackCron : configuredCron;
}
