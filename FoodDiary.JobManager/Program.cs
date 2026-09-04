using FoodDiary.Modules.OpenFoodFacts.Infrastructure;
using FoodDiary.Modules.Usda.Infrastructure;
using FoodDiary.Modules.Export.Infrastructure;
using FoodDiary.Modules.Dashboard.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure;
using FoodDiary.Application.Runtime;
using FoodDiary.Modules.Billing.Infrastructure;
using FoodDiary.Modules.Dietologist.Infrastructure;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Modules.Favorites.Infrastructure;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Application.Identity;
using FoodDiary.Application.Images;
using FoodDiary.Application.Notifications;
using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Modules.WeeklyGoals.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;
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
builder.Services.AddFavoritesModule();
builder.Services.AddGamificationModule();
builder.Services.AddIdentityModule();
builder.Services.AddImagesModule();
builder.Services.AddWeeklyGoalsModule();
builder.Services.AddNotificationsModule().AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddUsersModule();
builder.Services.AddBillingModule();
builder.Services.AddMarketingModule();
builder.Services.AddMealsPersistence();
builder.Services.AddRecentItemsModule();
builder.Services.AddInfrastructure(builder.Configuration).AddExportInfrastructure().AddAiPersistence().AddRecipesPersistence().AddAdminPersistence().AddIdentityPersistence().AddIdentityAuthenticationInfrastructure().AddProductsPersistence().AddDashboardReadServices();
builder.Services.AddImagesInfrastructure();
builder.Services.AddIntegrations(builder.Configuration).AddIdentityProvider(builder.Configuration).AddImagesProvider(builder.Configuration).AddAiProvider(builder.Configuration).AddUsdaProvider(builder.Configuration).AddOpenFoodFactsProvider(builder.Configuration).AddWearablesProvider(builder.Configuration);
builder.Services.AddDataProtection();
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
