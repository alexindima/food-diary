using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddRecentItemsModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, RecentItemsUserDataPurgeParticipant>());
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<RecentItemsDbContext>(static options => new RecentItemsDbContext(options)));
        services.AddScoped<IRecentItemRepository>(static provider => {
            FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
            return new RecentItemRepository(provider.GetRequiredService<RecentItemsDbContext>(),
                () => shared.Database.CurrentTransaction?.GetDbTransaction(), provider.GetRequiredService<TimeProvider>());
        });
        services.AddScoped<IRecentItemReadRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageReadService>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemWriteRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageRecorder, PostCommitRecentItemUsageRecorder>();
        return services;
    }
}
