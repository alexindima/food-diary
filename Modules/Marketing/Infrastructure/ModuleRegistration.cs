using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Application;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Marketing.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddMarketingModule(this IServiceCollection services) {
        services.AddMarketingApplication();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, MarketingUserDataPurgeParticipant>());
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<MarketingDbContext>(static options => new MarketingDbContext(options)));
        services.AddScoped(static provider => new MarketingAttributionEventRepository(
            provider.GetRequiredService<MarketingDbContext>().MarketingAttributionEvents));
        services.AddScoped<IMarketingAttributionEventRepository>(static provider => provider.GetRequiredService<MarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionRangeReadRepository>(static provider => provider.GetRequiredService<MarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventReadRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        services.AddScoped<IMarketingAttributionEventWriteRepository>(static provider => provider.GetRequiredService<IMarketingAttributionEventRepository>());
        return services;
    }
}
