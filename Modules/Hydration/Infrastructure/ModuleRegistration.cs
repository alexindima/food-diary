using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Hydration.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddHydrationModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, HydrationUserDataPurgeParticipant>());
        services.AddHydrationApplication();
        services.AddScoped<FoodDiary.Application.Hydration.Common.IHydrationIntervalReadService>(static provider => new HydrationIntervalReadService(
            provider.GetRequiredService<FoodDiaryDbContext>().HydrationEntries));
        services.AddScoped<IHydrationOperationReceiptRepository>(static provider => new HydrationOperationReceiptRepository(
            provider.GetRequiredService<FoodDiaryDbContext>().Set<FoodDiary.Domain.Entities.Tracking.HydrationOperationReceipt>()));
        services.AddScoped(static provider => new HydrationEntryRepository(
            provider.GetRequiredService<FoodDiaryDbContext>().HydrationEntries));
        services.AddScoped<IHydrationEntryReadModelRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        services.AddScoped<IHydrationEntryWriteRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        return services;
    }
}
