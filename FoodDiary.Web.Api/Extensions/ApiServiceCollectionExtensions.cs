using FoodDiary.Modules.Dashboard.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure;
using FoodDiary.Application.Runtime;
using FoodDiary.Application.Admin;
using FoodDiary.Application.Ai;
using FoodDiary.Modules.Billing.Infrastructure;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.Cycles.Infrastructure;
using FoodDiary.Application.Dashboard;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Dietologist.Infrastructure;
using FoodDiary.Modules.Exercises.Infrastructure;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Modules.Favorites.Infrastructure;
using FoodDiary.Application.Identity;
using FoodDiary.Application.Images;
using FoodDiary.Modules.Lessons.Infrastructure;
using FoodDiary.Application.Statistics;
using FoodDiary.Application.Meals;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Application.Tdee;
using FoodDiary.Application.Notifications;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure;
using FoodDiary.Application.Products;
using FoodDiary.Application.Recipes;
using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Application.Users;
using FoodDiary.Modules.Wearables.Infrastructure;
using FoodDiary.Modules.WeeklyGoals.Infrastructure;
using FoodDiary.Modules.Usda.Infrastructure;
using FoodDiary.Application.WeeklyCheckIn;
using FoodDiary.Modules.DailyAdvices.Infrastructure;
using FoodDiary.Modules.ContentReports.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Application.Export;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Resources.Notifications;
using FoodDiary.Resources.Reports;
using FoodDiary.Web.Api.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Extensions;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
                .AddApiSwagger()
                .AddConfiguredOpenTelemetry(configuration)
                .AddApiHealthChecks();
        }
        private IServiceCollection AddApplicationModules(IConfiguration configuration, IHostEnvironment? environment) {
            return services
                .AddApplicationRuntime()
                .AddAdminModule()
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
                .AddNotificationsModule().AddNotificationsInfrastructure(configuration)
                .AddOpenFoodFactsModule()
                .AddProductsModule()
                .AddRecipesModule()
                .AddUsersModule()
                .AddBillingModule()
                .AddMarketingModule()
                .AddInfrastructure(configuration)
                .AddDashboardReadServices()
                .AddImagesInfrastructure()
                .AddIntegrations(configuration)
                .AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>()
                .AddSingleton<IDiaryPdfReportTextProvider, DiaryPdfReportResourceTextProvider>()
                .AddNotificationTestScheduler()
                .AddApiDistributedCache(configuration, environment)
                .AddPresentationApi()
                .AddEndpointsApiExplorer();
        }
        private IServiceCollection AddNotificationTestScheduler() {
            services.AddSingleton<NotificationTestScheduler>();
            services.AddSingleton<INotificationTestScheduler>(static provider =>
                provider.GetRequiredService<NotificationTestScheduler>());
            services.AddHostedService(static provider =>
                provider.GetRequiredService<NotificationTestScheduler>());
            return services;
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
            services.Replace(ServiceDescriptor.Singleton<IAdminSsoCodeStore, RedisAdminSsoCodeStore>());

            return services;
        }
    }
}
