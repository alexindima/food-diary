using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Dietologist.Services;

namespace FoodDiary.JobManager.Services;

public static class JobManagerServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public void AddJobManagerServices(IConfiguration configuration) {
            services.AddJobManagerOptions(configuration);
            services.AddJobManagerJobs();
            services.AddJobExecutionState();
        }

        private void AddJobManagerOptions(IConfiguration configuration) {
            services.AddAchievementOutboxOptions(configuration);
            services.AddOptions<ImageCleanupOptions>()
                .Bind(configuration.GetSection(ImageCleanupOptions.SectionName))
                .Validate(ImageCleanupOptions.HasValidConfiguration,
                    "ImageCleanup configuration requires positive OlderThanHours/BatchSize and a non-empty Cron.")
                .ValidateOnStart();
            services.AddOptions<UserCleanupOptions>()
                .Bind(configuration.GetSection(UserCleanupOptions.SectionName))
                .Validate(UserCleanupOptions.HasValidConfiguration,
                    "UserCleanup configuration requires positive RetentionDays/BatchSize, a non-empty Cron, and a valid optional ReassignUserId GUID.")
                .ValidateOnStart();
            services.AddOptions<UserLoginEventCleanupOptions>()
                .Bind(configuration.GetSection(UserLoginEventCleanupOptions.SectionName))
                .Validate(UserLoginEventCleanupOptions.HasValidConfiguration,
                    "UserLoginEventCleanup configuration requires positive RetentionDays/BatchSize and a non-empty Cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<MarketingAttributionCleanupOptions>()
                .Bind(configuration.GetSection(MarketingAttributionCleanupOptions.SectionName))
                .Validate(MarketingAttributionCleanupOptions.HasValidConfiguration,
                    "MarketingAttributionCleanup configuration requires positive RetentionDays/BatchSize and a non-empty Cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<NotificationCleanupOptions>()
                .Bind(configuration.GetSection(NotificationCleanupOptions.SectionName))
                .Validate(NotificationCleanupOptions.HasValidConfiguration,
                    "NotificationCleanup configuration requires positive retention days/batch size and a non-empty Cron.")
                .ValidateOnStart();
            services.AddOptions<BillingRenewalOptions>()
                .Bind(configuration.GetSection(BillingRenewalOptions.SectionName))
                .Validate(BillingRenewalOptions.HasValidConfiguration,
                    "BillingRenewal configuration requires a provider, a positive batch size, and a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<FastingNotificationOptions>()
                .Bind(configuration.GetSection(FastingNotificationOptions.SectionName))
                .Validate(FastingNotificationOptions.HasValidConfiguration,
                    "FastingNotifications configuration requires a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<ImageObjectDeletionOutboxOptions>()
                .Bind(configuration.GetSection(ImageObjectDeletionOutboxOptions.SectionName))
                .Validate(ImageObjectDeletionOutboxOptions.HasValidConfiguration,
                    "ImageObjectDeletionOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<EmailOutboxOptions>()
                .Bind(configuration.GetSection(EmailOutboxOptions.SectionName))
                .Validate(EmailOutboxOptions.HasValidConfiguration,
                    "EmailOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<NotificationWebPushOutboxOptions>()
                .Bind(configuration.GetSection(NotificationWebPushOutboxOptions.SectionName))
                .Validate(NotificationWebPushOutboxOptions.HasValidConfiguration,
                    "NotificationWebPushOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<ClientTaskReminderOptions>()
                .Bind(configuration.GetSection(ClientTaskReminderOptions.SectionName))
                .Validate(
                    ClientTaskReminderOptions.HasValidConfiguration,
                    "ClientTaskReminders configuration requires a non-empty cron when enabled.")
                .ValidateOnStart();

        }

        private void AddAchievementOutboxOptions(IConfiguration configuration) {
            services.AddOptions<AchievementEvaluationOutboxOptions>()
                .Bind(configuration.GetSection(AchievementEvaluationOutboxOptions.SectionName))
                .Validate(AchievementEvaluationOutboxOptions.HasValidConfiguration,
                    "AchievementEvaluationOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
                .ValidateOnStart();
        }

        private void AddJobManagerJobs() {
            services.AddScoped<INotificationPusher, NoOpNotificationPusher>();
            services.AddTransient<ImageCleanupJob>();
            services.AddTransient<BillingRenewalJob>();
            services.AddTransient<BillingWebhookInboxJob>();
            services.AddTransient<PaddleNotificationRecoveryJob>();
            services.AddTransient<FastingNotificationJob>();
            services.AddTransient<ImageObjectDeletionOutboxJob>();
            services.AddTransient<EmailOutboxJob>();
            services.AddTransient<NotificationWebPushOutboxJob>();
            services.AddTransient<AchievementEvaluationOutboxJob>();
            services.AddTransient<NotificationCleanupJob>();
            services.AddTransient<UserCleanupJob>();
            services.AddTransient<UserLoginEventCleanupJob>();
            services.AddTransient<MarketingAttributionCleanupJob>();
            services.AddTransient<ClientTaskReminderJob>();
            services.AddScoped<ClientTaskDueReminderProcessor>();

        }

        private void AddJobExecutionState() {
            services.AddSingleton<IJobExecutionStateTracker, JobExecutionStateTracker>();
            services.AddSingleton<JobExecutionObserver>();
        }
    }
}
