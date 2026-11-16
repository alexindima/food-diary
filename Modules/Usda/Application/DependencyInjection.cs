using FluentValidation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Usda.Application.CatalogImport;
using FoodDiary.Modules.Usda.Contracts.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Usda.Application;

public static class DependencyInjection {
    public static IServiceCollection AddUsdaApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IUsdaCatalogReadService, UsdaCatalogReadService>();
        return services;
    }
}
