using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.BodyMetrics;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddBodyMetricsModule(this IServiceCollection services) {
        services.AddBodyMetricsApplication();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IWeightEntryReadRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryReadModelRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryWriteRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWaistEntryRepository, WaistEntryRepository>();
        services.AddScoped<IWaistEntryReadRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryReadModelRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryWriteRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        return services;
    }
}
