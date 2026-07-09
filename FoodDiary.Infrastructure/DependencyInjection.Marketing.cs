using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Infrastructure.Persistence.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddMarketingPersistence(this IServiceCollection services) {
        services.AddScoped<IMarketingAttributionEventRepository, MarketingAttributionEventRepository>();
        services.AddScoped<IMarketingAttributionEventReadRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventWriteRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());

        return services;
    }
}
