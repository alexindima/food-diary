using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
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
using FoodDiary.Mediator;

namespace FoodDiary.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddFoodDiaryMediator(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.AddScoped<IDashboardSnapshotBuilder, DashboardSnapshotBuilder>();
        services.AddScoped<IFastingAnalyticsService, FastingAnalyticsService>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IDietologistEmailSender, DietologistEmailSender>();
        services.AddScoped<BillingAccessService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();

        return services;
    }
}
