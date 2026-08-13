using FoodDiary.Application;
using FoodDiary.Application.Billing;
using FoodDiary.Application.BodyMetrics;
using FoodDiary.Application.Dietologist;
using FoodDiary.Application.Fasting;
using FoodDiary.Application.Favorites;
using FoodDiary.Application.Notifications;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Users;
using FoodDiary.Application.Wearables;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;
using System.Diagnostics.CodeAnalysis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddBodyMetricsModule();
builder.Services.AddDietologistModule();
builder.Services.AddFastingModule();
builder.Services.AddFavoritesModule();
builder.Services.AddWearablesModule();
builder.Services.AddNotificationsModule();
builder.Services.AddUsersModule();
builder.Services.AddBillingModule();
builder.Services.AddMarketingModule();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddNotificationResources();
builder.Services.AddJobManagerServices(builder.Configuration);
builder.Services.AddJobManagerOpenTelemetry(builder.Configuration);

builder.Services.AddHangfire((_, config) => {
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();
builder.Services.AddSingleton<IRecurringJobRegistrationVerifier, HangfireRecurringJobRegistrationVerifier>();
builder.Services.AddHostedService<RecurringJobsHostedService>();

IHost app = builder.Build();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
