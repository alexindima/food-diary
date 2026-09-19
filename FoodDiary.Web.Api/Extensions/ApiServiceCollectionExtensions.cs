using FoodDiary.Modules.WeeklyGoals.Presentation.Extensions;
using FoodDiary.Modules.WeeklyGoals.Infrastructure;
using FoodDiary.Modules.WeeklyCheckIn.Presentation.Extensions;
using FoodDiary.Modules.Wearables.Presentation.Extensions;
using FoodDiary.Modules.Wearables.Infrastructure;
using FoodDiary.Modules.Users.Presentation.Extensions;
using FoodDiary.Modules.Users.Infrastructure;
using FoodDiary.Modules.Usda.Presentation.Extensions;
using FoodDiary.Modules.Usda.Infrastructure;
using FoodDiary.Modules.Tdee.Presentation.Extensions;
using FoodDiary.Modules.Statistics.Presentation.Extensions;
using FoodDiary.Modules.Recipes.Presentation.Extensions;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.RecipeCommunity.Presentation.Extensions;
using FoodDiary.Modules.RecipeCommunity.Infrastructure;
using FoodDiary.Modules.RecentItems.Infrastructure;
using FoodDiary.Modules.Products.Presentation.Extensions;
using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.Meals.Infrastructure;
using FoodDiary.Modules.Meals.Presentation.Extensions;
using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Modules.Marketing.Presentation.Extensions;
using FoodDiary.Modules.Notifications.Infrastructure;
using FoodDiary.Modules.Notifications.Presentation.Extensions;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.MealPlanning.Presentation.Extensions;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure;
using FoodDiary.Modules.OpenFoodFacts.Presentation.Extensions;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Images.Presentation.Extensions;
using FoodDiary.Modules.Hydration.Presentation.Extensions;
using FoodDiary.Modules.Lessons.Presentation.Extensions;
using FoodDiary.Modules.Identity.Infrastructure;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Lessons.Infrastructure;
using FoodDiary.Modules.Gamification.Presentation.Extensions;
using FoodDiary.Modules.Identity.Presentation.Extensions;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Authentication.Infrastructure;
using FoodDiary.Modules.Favorites.Presentation.Extensions;
using FoodDiary.Modules.Fasting.Presentation.Extensions;
using FoodDiary.Modules.Export.Presentation.Extensions;
using FoodDiary.Modules.Export.Application;
using FoodDiary.Modules.Exercises.Presentation.Extensions;

using FoodDiary.Modules.Dashboard.Presentation.Extensions;
using FoodDiary.Modules.Dietologist.Presentation.Extensions;
using FoodDiary.Modules.Cycles.Presentation.Extensions;
using FoodDiary.Modules.ContentReports.Infrastructure;
using FoodDiary.Modules.ContentReports.Presentation.Extensions;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Modules.BodyMetrics.Presentation.Extensions;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.Billing.Infrastructure.Providers;
using FoodDiary.Modules.Billing.Infrastructure;
using FoodDiary.Modules.Billing.Presentation.Extensions;
using FoodDiary.Modules.Ai.Presentation.Extensions;
using FoodDiary.Modules.Ai.Infrastructure;
using FoodDiary.Modules.Admin.Presentation.Extensions;
using FoodDiary.Modules.Admin.Infrastructure;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Modules.Export.Infrastructure;

using FoodDiary.Application.Runtime;
using FoodDiary.Modules.Cycles.Infrastructure;
using FoodDiary.Modules.Dashboard.Application;

using FoodDiary.Modules.Dietologist.Infrastructure;
using FoodDiary.Modules.Exercises.Infrastructure;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Modules.Favorites.Infrastructure;
using FoodDiary.Modules.Identity.Application;
using FoodDiary.Modules.Images.Application;

using FoodDiary.Modules.Statistics.Application;

using FoodDiary.Modules.Tdee.Application;
using FoodDiary.Modules.Notifications.Application;

using FoodDiary.Modules.WeeklyCheckIn.Application;
using FoodDiary.Modules.DailyAdvices.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;

using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Modules.Admin.Infrastructure.Integrations;
using FoodDiary.Presentation.Api.Filters;
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
                .AddRecentItemsModule()
                .AddUsersModule().AddReadModelComposition()
                .AddBillingModule()
                .AddMarketingModule()
                .AddModulePresentations()
                .AddInfrastructure(configuration).AddOutboxProcessing(configuration).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement().AddSharedAuthentication(configuration).AddIdentityEmailOptions(configuration)
                .AddExportInfrastructure()
                .AddIdentityPersistence()
                .AddIdentityAuthenticationInfrastructure()

                .AddImagesInfrastructure()
                .AddBillingIntegrations(configuration)
                .AddAdminMailInboxIntegration(configuration)
                .AddAdminBugTriageIntegration(configuration)
                .AddMailRelayIntegration(configuration)
                .AddIdentityProvider(configuration).AddImagesProvider(configuration).AddAiProvider(configuration).AddUsdaProvider(configuration).AddOpenFoodFactsProvider(configuration).AddWearablesProvider(configuration)
                .AddNotificationResources()
                .AddExportResources()
                .AddNotificationTestScheduler()
                .AddApiDistributedCache(configuration, environment)
                .AddPresentationApi()
                .AddEndpointsApiExplorer();
        }
        private IServiceCollection AddModulePresentations() {
            return services
                .AddAdminPresentation()
                .AddAiPresentation()
                .AddIdentityPresentation()
                .AddBillingPresentation()
                .AddContentReportsPresentation()
                .AddCyclesPresentation()
                .AddDashboardPresentation()
                .AddDietologistPresentation()
                .AddExercisesPresentation()
                .AddExportPresentation()
                .AddFastingPresentation()
                .AddFavoritesPresentation()
                .AddGamificationPresentation()
                .AddUsersPresentation()
                .AddHydrationPresentation()
                .AddImagesPresentation()
                .AddLessonsPresentation()
                .AddMarketingPresentation()
                .AddMealPlanningPresentation()
                .AddMealsPresentation()
                .AddNotificationsPresentation()
                .AddOpenFoodFactsPresentation()
                .AddProductsPresentation()
                .AddRecipeCommunityPresentation()
                .AddRecipesPresentation()
                .AddStatisticsPresentation()
                .AddTdeePresentation()
                .AddUsdaPresentation()
                .AddBodyMetricsPresentation()
                .AddWearablesPresentation()
                .AddWeeklyCheckInPresentation()
                .AddWeeklyGoalsPresentation();
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
