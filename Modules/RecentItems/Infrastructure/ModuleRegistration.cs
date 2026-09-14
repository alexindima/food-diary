using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddRecentItemsModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, RecentItemsUserDataPurgeParticipant>());
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<RecentItemsDbContext>(static options => new RecentItemsDbContext(options)));
        services.AddScoped<IRecentItemRepository>(static provider => {
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new RecentItemRepository(provider.GetRequiredService<RecentItemsDbContext>(),
                () => coordinator.CurrentTransaction, provider.GetRequiredService<TimeProvider>());
        });
        services.AddScoped<IRecentItemReadRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageReadService>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemWriteRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageRecorder, PostCommitRecentItemUsageRecorder>();
        return services;
    }
}
