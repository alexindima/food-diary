using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddIntegrationOptions(configuration);
        services.AddMailIntegrations(configuration);
        services.AddBillingIntegrations();

        return services;
    }
}
