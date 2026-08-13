using FoodDiary.Application;
using FoodDiary.Application.Ai;
using FoodDiary.Application.Billing;
using FoodDiary.Application.BodyMetrics;
using FoodDiary.Application.Cycles;
using FoodDiary.Application.Dashboard;
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
using FoodDiary.Application.Gamification;
using FoodDiary.Application.Export;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Integrations;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Resources.Notifications;
using FoodDiary.Resources.Reports;
using FoodDiary.Web.Api.Services;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddApiServices(IConfiguration configuration, IHostEnvironment? environment = null) {
            return services
                .AddApplicationModules(configuration, environment)
                .AddApiOptions()
                .AddLocalization()
                .AddApiAuthentication()
                .AddApiHostServices()
                .AddApiDataProtection(configuration)
                .AddSingleton<IWearableTokenProtector, WearableTokenProtector>()
                .AddApiSwagger()
                .AddConfiguredOpenTelemetry()
                .AddApiHealthChecks();
        }
        private IServiceCollection AddApplicationModules(IConfiguration configuration, IHostEnvironment? environment) {
            return services
                .AddApplication()
                .AddAiModule()
                .AddBodyMetricsModule()
                .AddCyclesModule()
                .AddDashboardModule()
                .AddHydrationModule()
                .AddDietologistModule()
                .AddExercisesModule()
                .AddFastingModule()
                .AddFavoritesModule()
                .AddIdentityModule()
                .AddImagesModule()
                .AddLessonsModule()
                .AddStatisticsModule()
                .AddMealsModule()
                .AddMealPlanningModule()
                .AddRecipeCommunityModule()
                .AddTdeeModule()
                .AddWearablesModule()
                .AddWeeklyGoalsModule()
                .AddUsdaModule()
                .AddWeeklyCheckInModule()
                .AddDailyAdvicesModule()
                .AddContentReportsModule()
                .AddGamificationModule()
                .AddExportModule()
                .AddNotificationsModule()
                .AddOpenFoodFactsModule()
                .AddUsersModule()
                .AddBillingModule()
                .AddMarketingModule()
                .AddInfrastructure(configuration)
                .AddIntegrations(configuration)
                .AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>()
                .AddSingleton<IDiaryPdfReportTextProvider, DiaryPdfReportResourceTextProvider>()
                .AddSingleton<INotificationTestScheduler, NotificationTestScheduler>()
                .AddApiDistributedCache(configuration, environment)
                .AddPresentationApi()
                .AddEndpointsApiExplorer();
        }
        private IServiceCollection AddApiDistributedCache(IConfiguration configuration, IHostEnvironment? environment) {
            string? redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString)) {
                if (environment?.IsDevelopment() == false) {
                    throw new InvalidOperationException("ConnectionStrings:Redis is required outside Development.");
                }

                services.AddDistributedMemoryCache();
                return services;
            }

            var redisConnection = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton(_ => redisConnection.Value);
            services.AddStackExchangeRedisCache(options => {
                options.Configuration = redisConnectionString;
                options.InstanceName = "fooddiary:";
                options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection.Value);
            });
            services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

            return services;
        }
    }
}
