using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

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
builder.Services.AddOptions<NotificationCleanupOptions>()
    .Bind(builder.Configuration.GetSection(NotificationCleanupOptions.SectionName))
    .Validate(NotificationCleanupOptions.HasValidConfiguration,
        "NotificationCleanup configuration requires positive retention days/batch size and a non-empty Cron.")
    .ValidateOnStart();

builder.Services.AddHangfire((sp, config) => {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<ImageCleanupJob>();
builder.Services.AddSingleton<NotificationCleanupJob>();
builder.Services.AddSingleton<UserCleanupJob>();
builder.Services.AddSingleton<IJobExecutionStateTracker, JobExecutionStateTracker>();
builder.Services.AddSingleton<IRecurringJobRegistrationVerifier, HangfireRecurringJobRegistrationVerifier>();
builder.Services.AddHostedService<RecurringJobsHostedService>();

var app = builder.Build();

await app.RunAsync();
