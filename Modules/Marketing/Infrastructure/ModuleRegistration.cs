using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddMarketingModule(this IServiceCollection services) {
        services.AddMarketingApplication();
        services.AddScoped<IMarketingAttributionEventRepository, MarketingAttributionEventRepository>();
        services.AddScoped<IMarketingAttributionEventReadRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventWriteRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        return services;
    }
}
