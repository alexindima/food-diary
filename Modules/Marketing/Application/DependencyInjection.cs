using FoodDiary.Mediator;
using FoodDiary.Modules.Marketing.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMarketingApplication(this IServiceCollection services, bool attributionEnabled = false) {
        services.AddSingleton(new MarketingAttributionOptions(attributionEnabled));
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
