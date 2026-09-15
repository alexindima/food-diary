using FoodDiary.Modules.RecentItems.Application;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.RecentItems.Application.Abstractions.Common;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence.RecentItems;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.RecentItems.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddRecentItemsModule(this IServiceCollection services) {
        services.AddRecentItemsApplication();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, RecentItemsUserDataPurgeParticipant>());
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<RecentItemsDbContext>(static options => new RecentItemsDbContext(options)));
        services.AddScoped<IRecentItemRepository>(static provider => {
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new RecentItemRepository(provider.GetRequiredService<RecentItemsDbContext>(),
                () => coordinator.CurrentTransaction, provider.GetRequiredService<TimeProvider>());
        });
        services.AddScoped<IRecentItemReadRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemWriteRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageRecorder, PostCommitRecentItemUsageRecorder>();
        return services;
    }
}
