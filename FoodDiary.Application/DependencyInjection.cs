using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration.Services;
using FoodDiary.Application.Images.Services;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FluentValidation;
using System.Reflection;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Mediator;

namespace FoodDiary.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddFoodDiaryMediator(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CommandTransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();
        services.AddScoped<IAdminImpersonationUserService, AdminImpersonationUserService>();
        services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
        services.AddScoped<IAdminUserReadService, AdminUserReadService>();
        services.AddScoped<IAiUserContextService, AiUserContextService>();
        services.AddScoped<IAuthenticationUserLookupService, AuthenticationUserLookupService>();
        services.AddScoped<IAuthenticationUserMutationService, AuthenticationUserMutationService>();
        services.AddScoped<IAuthenticationUserRegistrationService, AuthenticationUserRegistrationService>();
        services.AddScoped<IConsumptionReadService, ConsumptionReadService>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.TryAddScoped<IDashboardStatisticsReadService, MediatorDashboardStatisticsReadService>();
        services.TryAddScoped<IDashboardBodyReadService, RepositoryDashboardBodyReadService>();
        services.TryAddScoped<IDashboardMealsReadService, MediatorDashboardMealsReadService>();
        services.TryAddScoped<IDashboardReadService, ComposedDashboardReadService>();
        services.AddScoped<IDashboardUserContextService, DashboardUserContextService>();
        services.AddScoped<IDashboardSectionDataLoader, DashboardSectionDataLoader>();
        services.AddScoped<IDashboardSnapshotBuilder>(static serviceProvider =>
            new DashboardSnapshotBuilder(
                serviceProvider.GetRequiredService<IDashboardSectionDataLoader>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DashboardSnapshotBuilder>>()));
        services.AddScoped<IFastingAnalyticsService, FastingAnalyticsService>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();
        services.AddScoped<IHydrationGoalService, HydrationGoalService>();
        services.AddScoped<IImageAssetAccessService, ImageAssetAccessService>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddScoped<INotificationUserAccessService, NotificationUserAccessService>();
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddScoped<INotificationFeedReadService, NotificationFeedReadService>();
        services.AddScoped<IWebPushSubscriptionReadService, WebPushSubscriptionReadService>();
        services.AddScoped<INotificationUserContextService, NotificationUserContextService>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        services.AddScoped<IGamificationUserProfileService, GamificationUserProfileService>();
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IDietologistEmailSender, DietologistEmailSender>();
        services.AddScoped<IExportDiaryReadService, ExportDiaryReadService>();
        services.AddScoped<IBillingUserContextService, BillingUserContextService>();
        services.AddScoped<IBillingUserLookupService, BillingUserLookupService>();
        services.AddScoped<BillingAccessService>();
        services.AddScoped<BillingWebhookPaymentRecorder>();
        services.AddScoped<BillingRenewalService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IDietologistUserLookupService, DietologistUserLookupService>();
        services.AddScoped<IDietologistUserContextService, DietologistUserContextService>();
        services.AddScoped<IOpenFoodFactsCachedProductSearch, OpenFoodFactsCachedProductSearch>();
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<UserContextService>();
        services.AddScoped<ICurrentUserAccessService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserContextService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<ITdeeUserProfileService, TdeeUserProfileService>();
        services.AddScoped<IWeeklyCheckInUserProfileService, WeeklyCheckInUserProfileService>();
        services.AddScoped<IUsdaDailyMicronutrientReadService, UsdaDailyMicronutrientReadService>();

        return services;
    }
}
