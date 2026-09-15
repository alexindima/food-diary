using FoodDiary.Modules.BodyMetrics.Application;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddBodyMetricsModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, BodyMetricsUserDataPurgeParticipant>());
        services.AddBodyMetricsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<BodyMetricsDbContext>(static options => new BodyMetricsDbContext(options)));
        services.AddScoped<WeightEntryRepository>(static provider => new WeightEntryRepository(
            provider.GetRequiredService<BodyMetricsDbContext>().WeightEntries));
        services.AddScoped<IWeightEntryReadModelRepository>(static provider => provider.GetRequiredService<WeightEntryRepository>());
        services.AddScoped<IWeightEntryWriteRepository>(static provider => provider.GetRequiredService<WeightEntryRepository>());
        services.AddScoped<WaistEntryRepository>(static provider => new WaistEntryRepository(
            provider.GetRequiredService<BodyMetricsDbContext>().WaistEntries));
        services.AddScoped<IWaistEntryReadModelRepository>(static provider => provider.GetRequiredService<WaistEntryRepository>());
        services.AddScoped<IWaistEntryWriteRepository>(static provider => provider.GetRequiredService<WaistEntryRepository>());
        return services;
    }
}
