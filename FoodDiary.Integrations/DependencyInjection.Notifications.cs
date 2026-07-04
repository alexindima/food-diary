using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddNotificationIntegrations(this IServiceCollection services) {
        services.AddSingleton<IWebPushClientAdapter, WebPushClientAdapter>();
        services.AddScoped<IWebPushNotificationSender, WebPushNotificationSender>();
        services.AddScoped<IWebPushConfigurationProvider, WebPushNotificationSender>();

        return services;
    }
}
