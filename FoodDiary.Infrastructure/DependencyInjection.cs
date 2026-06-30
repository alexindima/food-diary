using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.TryAddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddLogging();
        services.AddInfrastructureOptions(configuration);
        services.AddPersistence(configuration);
        services.AddFeatureRepositories();
        services.AddAuthenticationInfrastructure();
        services.AddBillingInfrastructure();
        services.AddExportInfrastructure();
        services.AddWearablesInfrastructure();

        return services;
    }
}
