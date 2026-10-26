using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.MealPlanning.Infrastructure;

namespace FoodDiary.JobManager.Services;

public static class JobManagerServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public void AddJobManagerServices(IConfiguration configuration) {
            services.AddMealPlanningPersistence();
            services.AddJobManagerOptions(configuration);
            services.AddJobManagerJobs();
            services.AddJobExecutionState();
        }

        private void AddJobManagerOptions(IConfiguration configuration) {
            services.AddAchievementOutboxOptions(configuration);
            services.AddSessionCleanupOptions(configuration);
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
            services.AddOptions<FastingTelemetryCleanupOptions>()
                .Bind(configuration.GetSection(FastingTelemetryCleanupOptions.SectionName))
                .Validate(FastingTelemetryCleanupOptions.HasValidConfiguration,
                    "FastingTelemetryCleanup configuration requires positive RetentionDays/BatchSize and a non-empty Cron when enabled.")
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
            services.AddReminderOptions(configuration);
        }

        private void AddReminderOptions(IConfiguration configuration) {
            services.AddOptions<ClientTaskReminderOptions>()
                .Bind(configuration.GetSection(ClientTaskReminderOptions.SectionName))
                .Validate(
                    ClientTaskReminderOptions.HasValidConfiguration,
                    "ClientTaskReminders configuration requires a non-empty cron when enabled.")
                .ValidateOnStart();
            services.AddOptions<WeeklyGoalReminderOptions>()
                .Bind(configuration.GetSection(WeeklyGoalReminderOptions.SectionName))
                .Validate(
                    WeeklyGoalReminderOptions.HasValidConfiguration,
                    "WeeklyGoalReminders configuration requires a non-empty cron when enabled.")
                .ValidateOnStart();
        }

        private void AddSessionCleanupOptions(IConfiguration configuration) =>
            services.AddOptions<UserSessionCleanupOptions>()
                .Bind(configuration.GetSection(UserSessionCleanupOptions.SectionName))
                .Validate(UserSessionCleanupOptions.HasValidConfiguration,
                    "UserSessionCleanup requires positive retention, batch size 1-1000, max batches 1-100 and a Cron when enabled.")
                .ValidateOnStart();

        private void AddAchievementOutboxOptions(IConfiguration configuration) {
            services.AddOptions<AchievementEvaluationOutboxOptions>()
                .Bind(configuration.GetSection(AchievementEvaluationOutboxOptions.SectionName))
                .Validate(AchievementEvaluationOutboxOptions.HasValidConfiguration,
                    "AchievementEvaluationOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
                .ValidateOnStart();
        }

        private void AddJobManagerJobs() {
            services.AddScoped<IEmailVerificationNotifier, NoOpEmailVerificationNotifier>();
            services.AddSingleton<INotificationTestScheduler, UnavailableNotificationTestScheduler>();
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
            services.AddTransient<AtomicCommandReceiptCleanupJob>();
            services.AddTransient<UserCleanupJob>();
            services.AddTransient<UserLoginEventCleanupJob>();
            services.AddTransient<UserSessionCleanupJob>();
            services.AddTransient<MarketingAttributionCleanupJob>();
            services.AddTransient<FastingTelemetryCleanupJob>();
            services.AddTransient<ClientTaskReminderJob>();
            services.AddTransient<WeeklyGoalReminderJob>();

        }

        private void AddJobExecutionState() {
            services.AddSingleton<IJobExecutionStateTracker, JobExecutionStateTracker>();
            services.AddSingleton<JobExecutionObserver>();
        }
    }
}
