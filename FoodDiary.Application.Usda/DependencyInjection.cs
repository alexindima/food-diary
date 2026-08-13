using FluentValidation;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Usda;

public static class DependencyInjection {
    public static IServiceCollection AddUsdaModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IUsdaDailyMicronutrientReadService, UsdaDailyMicronutrientReadService>();
        services.AddScoped<IUsdaFoodReadService, UsdaFoodReadService>();
        services.AddScoped<IUsdaProductSuggestionReadService, UsdaProductSuggestionReadService>();
        return services;
    }
}
