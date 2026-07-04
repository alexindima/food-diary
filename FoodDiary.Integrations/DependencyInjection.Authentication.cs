using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddAuthenticationIntegrations(this IServiceCollection services) {
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();

        return services;
    }
}
