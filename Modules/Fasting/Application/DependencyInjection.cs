using FluentValidation;
using FoodDiary.Modules.Fasting.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Fasting.Application;

public static class DependencyInjection {
    public static IServiceCollection AddFastingApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IFastingAnalyticsService, FastingAnalyticsService>();
        services.AddScoped<IFastingReadService, FastingReadService>();
        services.AddScoped<IFastingTelemetrySummaryReadService, FastingTelemetrySummaryReadService>();
        services.AddScoped<IFastingTelemetryCleanupService, FastingTelemetryCleanupService>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();

        return services;
    }
}
