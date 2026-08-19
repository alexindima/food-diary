using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static void AddNotificationIntegrations(this IServiceCollection services) {
        services.AddTransient<WebPushEndpointValidationHandler>();
        services.AddHttpClient<IWebPushClientAdapter, WebPushClientAdapter>(client =>
                client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(WebPushSocketsHttpHandlerFactory.Create)
            .AddHttpMessageHandler<WebPushEndpointValidationHandler>();
        services.AddScoped<IWebPushNotificationSender, WebPushNotificationSender>();
        services.AddScoped<IWebPushConfigurationProvider, WebPushNotificationSender>();
    }
}
