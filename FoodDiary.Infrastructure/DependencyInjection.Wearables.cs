using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Wearables;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddWearablesInfrastructure(this IServiceCollection services) {
        services.AddScoped<IWearableConnectionRepository, WearableConnectionRepository>();
        services.AddScoped<IWearableConnectionReadRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableConnectionWriteRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableSyncRepository, WearableSyncRepository>();
        services.AddScoped<IWearableSyncReadRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncReadModelRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncWriteRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddSingleton<IWearableOAuthStateService, WearableOAuthStateService>();

        return services;
    }
}
