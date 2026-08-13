using FluentValidation;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.OpenFoodFacts;

public static class DependencyInjection {
    public static IServiceCollection AddOpenFoodFactsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IOpenFoodFactsCachedProductSearch, OpenFoodFactsCachedProductSearch>();
        return services;
    }
}
