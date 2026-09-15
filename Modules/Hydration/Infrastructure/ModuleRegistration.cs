using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Hydration.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddHydrationModule(this IServiceCollection services) {
        services.AddScoped<IHydrationOperationTransactionRunner, EfHydrationOperationTransactionRunner>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, HydrationUserDataPurgeParticipant>());
        services.AddHydrationApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<HydrationDbContext>(static options => new HydrationDbContext(options)));
        services.AddScoped<FoodDiary.Modules.Hydration.Application.Abstractions.Common.IHydrationIntervalReadModelRepository>(static provider => new HydrationIntervalReadService(
            provider.GetRequiredService<HydrationDbContext>().HydrationEntries));
        services.AddScoped<IHydrationOperationReceiptRepository>(static provider => new HydrationOperationReceiptRepository(
            provider.GetRequiredService<HydrationDbContext>().HydrationOperationReceipts));
        services.AddScoped(static provider => new HydrationEntryRepository(
            provider.GetRequiredService<HydrationDbContext>().HydrationEntries));
        services.AddScoped<IHydrationEntryReadModelRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        services.AddScoped<IHydrationEntryWriteRepository>(static provider => provider.GetRequiredService<HydrationEntryRepository>());
        return services;
    }
}
