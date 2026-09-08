using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddMarketingModule(this IServiceCollection services) {
        services.AddMarketingApplication();
        services.AddScoped<MarketingAttributionEventRepository>();
        services.AddScoped<IMarketingAttributionEventRepository>(static provider => provider.GetRequiredService<MarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionRangeReadRepository>(static provider => provider.GetRequiredService<MarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventReadRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventWriteRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        return services;
    }
}
