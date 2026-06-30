using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Images.Services;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FluentValidation;
using System.Reflection;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Mediator;

namespace FoodDiary.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddFoodDiaryMediator(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PostCommitBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.AddScoped<IDashboardStatisticsReadService, MediatorDashboardStatisticsReadService>();
        services.AddScoped<IDashboardBodyReadService, RepositoryDashboardBodyReadService>();
        services.AddScoped<IDashboardMealsReadService, MediatorDashboardMealsReadService>();
        services.AddScoped<IDashboardSectionDataLoader, DashboardSectionDataLoader>();
        services.AddScoped<IDashboardSnapshotBuilder>(static serviceProvider =>
            new DashboardSnapshotBuilder(
                serviceProvider.GetRequiredService<IDashboardSectionDataLoader>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DashboardSnapshotBuilder>>()));
        services.AddScoped<IFastingAnalyticsService, FastingAnalyticsService>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();
        services.AddScoped<IImageAssetAccessService, ImageAssetAccessService>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IDietologistEmailSender, DietologistEmailSender>();
        services.AddScoped<BillingAccessService>();
        services.AddScoped<BillingRenewalService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IOpenFoodFactsCachedProductSearch, OpenFoodFactsCachedProductSearch>();
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();

        return services;
    }
}
