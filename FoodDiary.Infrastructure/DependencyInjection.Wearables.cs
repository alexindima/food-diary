using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Wearables;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddWearablesInfrastructure(this IServiceCollection services) {
        services.AddScoped<IWearableConnectionRepository, WearableConnectionRepository>();
        services.AddScoped<IWearableSyncRepository, WearableSyncRepository>();
        services.AddSingleton<IWearableOAuthStateService, WearableOAuthStateService>();

        return services;
    }
}
