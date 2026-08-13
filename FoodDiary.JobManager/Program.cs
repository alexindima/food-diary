using FoodDiary.Application;
using FoodDiary.Application.Ai;
using FoodDiary.Application.Billing;
using FoodDiary.Application.BodyMetrics;
using FoodDiary.Application.Cycles;
using FoodDiary.Application.Hydration;
using FoodDiary.Application.Dietologist;
using FoodDiary.Application.Exercises;
using FoodDiary.Application.Fasting;
using FoodDiary.Application.Favorites;
using FoodDiary.Application.Identity;
using FoodDiary.Application.Images;
using FoodDiary.Application.Lessons;
using FoodDiary.Application.Statistics;
using FoodDiary.Application.Meals;
using FoodDiary.Application.MealPlanning;
using FoodDiary.Application.RecipeCommunity;
using FoodDiary.Application.Tdee;
using FoodDiary.Application.Notifications;
using FoodDiary.Application.OpenFoodFacts;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Users;
using FoodDiary.Application.Wearables;
using FoodDiary.Application.WeeklyGoals;
using FoodDiary.Application.Usda;
using FoodDiary.Application.WeeklyCheckIn;
using FoodDiary.Application.DailyAdvices;
using FoodDiary.Application.ContentReports;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;
using System.Diagnostics.CodeAnalysis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddAiModule();
builder.Services.AddBodyMetricsModule();
builder.Services.AddCyclesModule();
builder.Services.AddHydrationModule();
builder.Services.AddDietologistModule();
builder.Services.AddExercisesModule();
builder.Services.AddFastingModule();
builder.Services.AddFavoritesModule();
builder.Services.AddIdentityModule();
builder.Services.AddImagesModule();
builder.Services.AddLessonsModule();
builder.Services.AddStatisticsModule();
builder.Services.AddMealsModule();
builder.Services.AddMealPlanningModule();
builder.Services.AddRecipeCommunityModule();
builder.Services.AddTdeeModule();
builder.Services.AddWearablesModule();
builder.Services.AddWeeklyGoalsModule();
builder.Services.AddUsdaModule();
builder.Services.AddWeeklyCheckInModule();
builder.Services.AddDailyAdvicesModule();
builder.Services.AddContentReportsModule();
builder.Services.AddNotificationsModule();
builder.Services.AddOpenFoodFactsModule();
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
