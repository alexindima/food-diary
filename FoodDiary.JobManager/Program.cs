using FoodDiary.Application.Runtime;
using FoodDiary.Application.Billing;
using FoodDiary.Application.Dietologist;
using FoodDiary.Application.Fasting;
using FoodDiary.Application.Gamification;
using FoodDiary.Application.Identity;
using FoodDiary.Application.Images;
using FoodDiary.Application.Notifications;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Meals;
using FoodDiary.Application.Users;
using FoodDiary.Application.WeeklyGoals;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;
using System.Diagnostics.CodeAnalysis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationRuntime();
builder.Services.AddDietologistModule();
builder.Services.AddFastingModule();
builder.Services.AddGamificationModule();
builder.Services.AddIdentityModule();
builder.Services.AddImagesModule();
builder.Services.AddWeeklyGoalsModule();
builder.Services.AddNotificationsModule();
builder.Services.AddUsersModule();
builder.Services.AddBillingModule();
builder.Services.AddMarketingModule();
builder.Services.AddMealsModule();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddNotificationResources();
builder.Services.AddJobManagerServices(builder.Configuration);
builder.Services.AddJobManagerOpenTelemetry(builder.Configuration);

builder.Services.AddHangfire((_, config) => {
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString)) {
        throw new InvalidOperationException("DefaultConnection must be supplied through environment variables or user secrets.");
    }

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
