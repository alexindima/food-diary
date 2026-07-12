using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Marketing;

public static class DependencyInjection {
    public static IServiceCollection AddMarketingModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<MarketingConversionRecorder>();
        services.AddScoped<IMarketingConversionRecorder>(static provider =>
            provider.GetRequiredService<MarketingConversionRecorder>());
        services.AddScoped<IBillingMarketingConversionRecorder>(static provider =>
            provider.GetRequiredService<MarketingConversionRecorder>());
        services.AddScoped<IMarketingAttributionCleanupService, MarketingAttributionCleanupService>();
        services.AddScoped<IMarketingAttributionSummaryReadService, MarketingAttributionSummaryReadService>();

        return services;
    }
}
