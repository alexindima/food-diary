using FoodDiary.Application.Abstractions.Notifications.Common;

namespace FoodDiary.JobManager.Services;

public static class JobManagerServiceCollectionExtensions {
    public static IServiceCollection AddJobManagerServices(this IServiceCollection services, IConfiguration configuration) {
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
        services.AddOptions<NotificationWebPushOutboxOptions>()
            .Bind(configuration.GetSection(NotificationWebPushOutboxOptions.SectionName))
            .Validate(NotificationWebPushOutboxOptions.HasValidConfiguration,
                "NotificationWebPushOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
            .ValidateOnStart();

        services.AddScoped<INotificationPusher, NoOpNotificationPusher>();
        services.AddTransient<ImageCleanupJob>();
        services.AddTransient<BillingRenewalJob>();
        services.AddTransient<FastingNotificationJob>();
        services.AddTransient<ImageObjectDeletionOutboxJob>();
        services.AddTransient<NotificationWebPushOutboxJob>();
        services.AddTransient<NotificationCleanupJob>();
        services.AddTransient<UserCleanupJob>();
        services.AddTransient<UserLoginEventCleanupJob>();
        services.AddSingleton<IJobExecutionStateTracker, JobExecutionStateTracker>();

        return services;
    }
}
