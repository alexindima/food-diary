using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Marketing;

public static class DependencyInjection {
    public static IServiceCollection AddMarketingApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IMarketingAttributionCleanupService, MarketingAttributionCleanupService>();
        services.AddScoped<IMarketingAttributionSummaryReadService, MarketingAttributionSummaryReadService>();

        services.AddScoped<IMarketingAttributionRangeReadService, MarketingAttributionRangeReadService>();

        return services;
    }
}
