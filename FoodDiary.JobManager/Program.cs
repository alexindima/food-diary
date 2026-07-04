using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;
using System.Diagnostics.CodeAnalysis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);

builder.Services.AddOptions<ImageCleanupOptions>()
    .Bind(builder.Configuration.GetSection(ImageCleanupOptions.SectionName))
    .Validate(ImageCleanupOptions.HasValidConfiguration,
        "ImageCleanup configuration requires positive OlderThanHours/BatchSize and a non-empty Cron.")
    .ValidateOnStart();
builder.Services.AddOptions<UserCleanupOptions>()
    .Bind(builder.Configuration.GetSection(UserCleanupOptions.SectionName))
    .Validate(UserCleanupOptions.HasValidConfiguration,
        "UserCleanup configuration requires positive RetentionDays/BatchSize, a non-empty Cron, and a valid optional ReassignUserId GUID.")
    .ValidateOnStart();
builder.Services.AddOptions<UserLoginEventCleanupOptions>()
    .Bind(builder.Configuration.GetSection(UserLoginEventCleanupOptions.SectionName))
    .Validate(UserLoginEventCleanupOptions.HasValidConfiguration,
        "UserLoginEventCleanup configuration requires positive RetentionDays/BatchSize and a non-empty Cron when enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<NotificationCleanupOptions>()
    .Bind(builder.Configuration.GetSection(NotificationCleanupOptions.SectionName))
    .Validate(NotificationCleanupOptions.HasValidConfiguration,
        "NotificationCleanup configuration requires positive retention days/batch size and a non-empty Cron.")
    .ValidateOnStart();
builder.Services.AddOptions<BillingRenewalOptions>()
    .Bind(builder.Configuration.GetSection(BillingRenewalOptions.SectionName))
    .Validate(BillingRenewalOptions.HasValidConfiguration,
        "BillingRenewal configuration requires a provider, a positive batch size, and a non-empty cron when enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<FastingNotificationOptions>()
    .Bind(builder.Configuration.GetSection(FastingNotificationOptions.SectionName))
    .Validate(FastingNotificationOptions.HasValidConfiguration,
        "FastingNotifications configuration requires a non-empty cron when enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<ImageObjectDeletionOutboxOptions>()
    .Bind(builder.Configuration.GetSection(ImageObjectDeletionOutboxOptions.SectionName))
    .Validate(ImageObjectDeletionOutboxOptions.HasValidConfiguration,
        "ImageObjectDeletionOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<NotificationWebPushOutboxOptions>()
    .Bind(builder.Configuration.GetSection(NotificationWebPushOutboxOptions.SectionName))
    .Validate(NotificationWebPushOutboxOptions.HasValidConfiguration,
        "NotificationWebPushOutbox configuration requires a positive batch size and a non-empty cron when enabled.")
    .ValidateOnStart();

builder.Services.AddHangfire((sp, config) => {
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();

builder.Services.AddTransient<ImageCleanupJob>();
builder.Services.AddTransient<BillingRenewalJob>();
builder.Services.AddTransient<FastingNotificationJob>();
builder.Services.AddTransient<ImageObjectDeletionOutboxJob>();
builder.Services.AddTransient<NotificationWebPushOutboxJob>();
builder.Services.AddTransient<NotificationCleanupJob>();
builder.Services.AddTransient<UserCleanupJob>();
builder.Services.AddTransient<UserLoginEventCleanupJob>();
builder.Services.AddSingleton<IJobExecutionStateTracker, JobExecutionStateTracker>();
builder.Services.AddSingleton<IRecurringJobRegistrationVerifier, HangfireRecurringJobRegistrationVerifier>();
builder.Services.AddHostedService<RecurringJobsHostedService>();

IHost app = builder.Build();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
