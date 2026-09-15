using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Wearables.Infrastructure.Persistence;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application;
using FoodDiary.Modules.Wearables.Infrastructure.Authentication;
using FoodDiary.Modules.Wearables.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Wearables.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddWearablesModule(this IServiceCollection services) {
        services.AddWearablesApplication();
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<WearablesDbContext>(options => new WearablesDbContext(options)));
        services.AddScoped<IWearableConnectionRepository>(provider =>
            new WearableConnectionRepository(provider.GetRequiredService<WearablesDbContext>().WearableConnections));
        services.AddScoped<IWearableConnectionReadRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableConnectionWriteRepository>(static provider => provider.GetRequiredService<IWearableConnectionRepository>());
        services.AddScoped<IWearableTransactionRunner, EfWearableTransactionRunner>();
        services.AddScoped<IWearableSyncRepository>(provider =>
            new WearableSyncRepository(provider.GetRequiredService<WearablesDbContext>().WearableSyncEntries));
        services.AddScoped<IWearableSyncReadRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncReadModelRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddScoped<IWearableSyncWriteRepository>(static provider => provider.GetRequiredService<IWearableSyncRepository>());
        services.AddSingleton<IWearableOAuthStateService, WearableOAuthStateService>();
        services.AddSingleton<IWearableTokenProtector, WearableTokenProtector>();
        return services;
    }
}
