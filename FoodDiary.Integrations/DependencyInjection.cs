using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddIntegrationOptions(configuration);
        services.AddStorageIntegrations();
        services.AddMailIntegrations(configuration);
        services.AddAuthenticationIntegrations();
        services.AddBillingIntegrations();
        services.AddNotificationIntegrations();
        services.AddAiIntegrations();
        services.AddFoodDataIntegrations(configuration);
        services.AddWearableIntegrations();

        return services;
    }
}
