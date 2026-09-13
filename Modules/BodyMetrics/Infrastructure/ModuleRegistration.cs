using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.BodyMetrics;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddBodyMetricsModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, BodyMetricsUserDataPurgeParticipant>());
        services.AddBodyMetricsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<BodyMetricsDbContext>(static options => new BodyMetricsDbContext(options)));
        services.AddScoped<IWeightEntryRepository>(static provider => new WeightEntryRepository(
            provider.GetRequiredService<BodyMetricsDbContext>().WeightEntries));
        services.AddScoped<IWeightEntryReadRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryReadModelRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryWriteRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWaistEntryRepository>(static provider => new WaistEntryRepository(
            provider.GetRequiredService<BodyMetricsDbContext>().WaistEntries));
        services.AddScoped<IWaistEntryReadRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryReadModelRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryWriteRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        return services;
    }
}
