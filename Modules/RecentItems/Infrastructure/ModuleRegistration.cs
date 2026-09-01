using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddRecentItemsModule(this IServiceCollection services) {
        services.AddScoped<IRecentItemRepository, RecentItemRepository>();
        services.AddScoped<IRecentItemReadRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageReadService>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemWriteRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemUsageRecorder, PostCommitRecentItemUsageRecorder>();
        return services;
    }
}
