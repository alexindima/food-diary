using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Marketing.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMarketingApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
