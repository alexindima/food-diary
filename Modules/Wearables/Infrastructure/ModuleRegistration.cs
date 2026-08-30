using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Wearables;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Wearables;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Wearables.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddWearablesModule(this IServiceCollection services) {
        services.AddWearablesApplication();
        services.AddScoped<IWearableConnectionRepository, WearableConnectionRepository>();
        services.AddScoped<IWearableConnectionReadRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableConnectionWriteRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableTransactionRunner, EfWearableTransactionRunner>();
        services.AddScoped<IWearableSyncRepository, WearableSyncRepository>();
        services.AddScoped<IWearableSyncReadRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncReadModelRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncWriteRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddSingleton<IWearableOAuthStateService, WearableOAuthStateService>();
        services.AddSingleton<IWearableTokenProtector, WearableTokenProtector>();
        return services;
    }
}
