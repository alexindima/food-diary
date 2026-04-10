using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Images.Services;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FluentValidation;
using System.Reflection;

namespace FoodDiary.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.AddScoped<IDashboardSnapshotBuilder, DashboardSnapshotBuilder>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();

        return services;
    }
}
