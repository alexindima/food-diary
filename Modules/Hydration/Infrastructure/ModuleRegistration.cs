using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Hydration.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddHydrationModule(this IServiceCollection services) {
        services.AddHydrationApplication();
        services.AddScoped(static provider => new HydrationEntryRepository(
            provider.GetRequiredService<FoodDiaryDbContext>().HydrationEntries));
        services.AddScoped<IHydrationEntryReadModelRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        services.AddScoped<IHydrationEntryWriteRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        return services;
    }
}
